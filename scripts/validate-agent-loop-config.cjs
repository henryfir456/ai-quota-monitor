'use strict';

const path = require('node:path');
const CAPABILITIES = Object.freeze([
  'scan', 'issue_claim', 'issue_write', 'develop', 'review', 'merge', 'close_issue', 'deploy'
]);

function isRecord(value) {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function exactKeys(value, expected, path, errors) {
  if (!isRecord(value)) {
    errors.push(`${path} must be an object`);
    return false;
  }
  const allowed = new Set(expected);
  for (const key of Object.keys(value)) {
    if (!allowed.has(key)) errors.push(`${path}.${key} is not allowed`);
  }
  for (const key of expected) {
    if (!Object.hasOwn(value, key)) errors.push(`${path}.${key} is required`);
  }
  return true;
}

function integer(value, min, max, path, errors) {
  if (!Number.isSafeInteger(value) || value < min || value > max) {
    errors.push(`${path} must be a finite integer from ${min} through ${max}`);
    return false;
  }
  return true;
}

function boolean(value, path, errors) {
  if (typeof value !== 'boolean') errors.push(`${path} must be a boolean`);
}

function safeRelativePath(value) {
  return typeof value === 'string' && value.trim().length > 0 && !value.includes('\0') &&
    !path.posix.isAbsolute(value) && !path.win32.isAbsolute(value) && !/^[A-Za-z]:/.test(value) &&
    !value.split(/[\\/]/).includes('..');
}

function validateProjectConfig(value) {
  const errors = [];
  if (!exactKeys(value, ['schema_version', 'controller', 'projects'], 'config', errors)) {
    return { valid: false, errors };
  }
  if (value.schema_version !== 1) errors.push('schema_version must be 1');

  const controller = value.controller;
  if (exactKeys(controller, ['state_path', 'scheduler', 'dispatcher', 'limits'], 'controller', errors)) {
    if (!safeRelativePath(controller.state_path)) {
      errors.push('controller.state_path must be a non-empty relative path without parent traversal');
    }
    if (exactKeys(controller.scheduler, ['enabled', 'interval_seconds'], 'controller.scheduler', errors)) {
      boolean(controller.scheduler.enabled, 'controller.scheduler.enabled', errors);
      integer(controller.scheduler.interval_seconds, 1, 86400, 'controller.scheduler.interval_seconds', errors);
    }
    if (exactKeys(controller.dispatcher, ['enabled'], 'controller.dispatcher', errors)) {
      boolean(controller.dispatcher.enabled, 'controller.dispatcher.enabled', errors);
    }
    if (exactKeys(controller.limits, [
      'max_parallel_jobs', 'max_parallel_per_repo', 'daily_dispatch_budget', 'max_issue_pages_per_repo'
    ], 'controller.limits', errors)) {
      integer(controller.limits.max_parallel_jobs, 1, 100, 'controller.limits.max_parallel_jobs', errors);
      integer(controller.limits.max_parallel_per_repo, 1, 10, 'controller.limits.max_parallel_per_repo', errors);
      integer(controller.limits.daily_dispatch_budget, 0, 100000, 'controller.limits.daily_dispatch_budget', errors);
      integer(controller.limits.max_issue_pages_per_repo, 1, 20, 'controller.limits.max_issue_pages_per_repo', errors);
      if (Number.isSafeInteger(controller.limits.max_parallel_per_repo) &&
          Number.isSafeInteger(controller.limits.max_parallel_jobs) &&
          controller.limits.max_parallel_per_repo > controller.limits.max_parallel_jobs) {
        errors.push('controller.limits.max_parallel_per_repo cannot exceed max_parallel_jobs');
      }
    }
  }

  if (!Array.isArray(value.projects) || value.projects.length === 0) {
    errors.push('projects must be a non-empty array');
  } else {
    const seen = new Set();
    value.projects.forEach((project, index) => {
      const prefix = `projects[${index}]`;
      if (!exactKeys(project, ['repo', 'enabled', 'capabilities', 'limits', 'runtime', 'dependencies', 'test_commands'], prefix, errors)) return;
      if (typeof project.repo !== 'string' || !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(project.repo)) {
        errors.push(`${prefix}.repo must be owner/repository`);
      } else if (seen.has(project.repo.toLowerCase())) {
        errors.push(`${prefix}.repo is duplicated`);
      } else {
        seen.add(project.repo.toLowerCase());
      }
      boolean(project.enabled, `${prefix}.enabled`, errors);

      const capabilities = project.capabilities;
      if (exactKeys(capabilities, CAPABILITIES, `${prefix}.capabilities`, errors)) {
        for (const name of CAPABILITIES) boolean(capabilities[name], `${prefix}.capabilities.${name}`, errors);
      }
      const limits = project.limits;
      if (exactKeys(limits, ['max_parallel_jobs', 'daily_dispatch_budget'], `${prefix}.limits`, errors)) {
        integer(limits.max_parallel_jobs, 1, 10, `${prefix}.limits.max_parallel_jobs`, errors);
        integer(limits.daily_dispatch_budget, 0, 100000, `${prefix}.limits.daily_dispatch_budget`, errors);
      }
      const runtime = project.runtime;
      if (exactKeys(runtime, ['name', 'minimum_version'], `${prefix}.runtime`, errors)) {
        if (typeof runtime.name !== 'string' || runtime.name.trim().length === 0) errors.push(`${prefix}.runtime.name is required`);
        if (typeof runtime.minimum_version !== 'string' || !/^\d+\.\d+(?:\.\d+)?$/.test(runtime.minimum_version)) {
          errors.push(`${prefix}.runtime.minimum_version must be a numeric version`);
        }
      }
      for (const field of ['dependencies', 'test_commands']) {
        if (!Array.isArray(project[field]) || project[field].some((entry) => typeof entry !== 'string' || entry.trim().length === 0)) {
          errors.push(`${prefix}.${field} must be an array of non-empty strings`);
        }
      }

      if (isRecord(capabilities) && isRecord(limits)) {
        if (capabilities.issue_write && !capabilities.issue_claim) errors.push(`${prefix}.issue_write requires issue_claim`);
        if (capabilities.develop || capabilities.review) {
          if (!capabilities.issue_claim) errors.push(`${prefix}.develop/review requires issue_claim`);
          if (controller?.dispatcher?.enabled !== true) errors.push(`${prefix}.develop/review requires an enabled dispatcher`);
          if (!(controller?.limits?.daily_dispatch_budget > 0) || !(limits.daily_dispatch_budget > 0)) {
            errors.push(`${prefix}.develop/review requires positive global and project daily budgets`);
          }
          if (!(controller?.limits?.max_parallel_jobs > 0) || !(limits.max_parallel_jobs > 0)) {
            errors.push(`${prefix}.develop/review requires positive global and project job limits`);
          }
        }
        if (capabilities.merge && (!capabilities.review || !capabilities.issue_write)) {
          errors.push(`${prefix}.merge requires review and issue_write`);
        }
        if (capabilities.close_issue && !capabilities.merge) errors.push(`${prefix}.close_issue requires merge`);
        if (capabilities.deploy && !capabilities.merge) errors.push(`${prefix}.deploy requires merge`);
      }
    });
  }
  return { valid: errors.length === 0, errors };
}

module.exports = { validateProjectConfig };
