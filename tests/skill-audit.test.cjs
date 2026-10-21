'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const {spawnSync} = require('node:child_process');
const script = path.resolve(__dirname, '../scripts/skill-audit.cjs');
function run(files = {}, args = []) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'skill-audit-test-'));
  try {
    for (const [name, content] of Object.entries(files)) {
      const p = path.join(dir,name);
      fs.mkdirSync(path.dirname(p),{recursive:true});
      fs.writeFileSync(p,content);
    }
    const child = spawnSync(process.execPath,[script,...args,'--out','reports/check'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(child.error, undefined, child.error?.message);
    const reportFile=path.join(dir,'reports/check.json');
    const mdFile=path.join(dir,'reports/check.md');
    return {status:child.status, error:child.error, stderr:child.stderr, stdout:child.stdout, report:fs.existsSync(reportFile)?JSON.parse(fs.readFileSync(reportFile,'utf8')):null, markdown:fs.existsSync(mdFile)?fs.readFileSync(mdFile,'utf8'):null};
  } finally { fs.rmSync(dir,{recursive:true,force:true}); }
}
test('missing manifest reports NOT_APPLICABLE with zero additional model calls',()=>{
  const r=run(); assert.equal(r.status,0); assert.equal(r.report.findings[0].status,'NOT_APPLICABLE');
  assert.equal(r.report.telemetry.additionalModelCalls,0); assert.equal(r.report.telemetry.tokenStatus,'UNKNOWN');
  assert.match(r.markdown,/UNVERIFIED/);
});
test('manifest directory is UNVERIFIED instead of crashing',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-manifest-directory-'));
  try {
    fs.mkdirSync(path.join(dir,'manifest-dir'));
    const result=spawnSync(process.execPath,[script,'--manifest','manifest-dir','--out','reports/audit'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(result.error, undefined, result.error?.message);
    assert.equal(result.status,0);
    const report=JSON.parse(fs.readFileSync(path.join(dir,'reports/audit.json'),'utf8'));
    assert.ok(report.findings.some(x=>x.code==='MANIFEST_UNREADABLE'&&x.status==='UNVERIFIED'));
    assert.equal(report.aiReview.recommendation,'CONDITIONAL_REVIEW');
  } finally {fs.rmSync(dir,{recursive:true,force:true});}
});
test('literal skill path resolves and hashes deterministically',()=>{
  const files={'agent.yaml':'skills:\n  - path: .agents/skills/a/SKILL.md\n','.agents/skills/a/SKILL.md':'# Example\n'};
  const r=run(files);assert.equal(r.status,0);assert.equal(r.report.skills.length,1);
  assert.match(r.report.skills[0].sha256,/^[0-9a-f]{64}$/);
  assert.equal(r.report.skills[0].sha256,crypto.createHash('sha256').update('# Example\n').digest('hex'));
  assert.equal(r.report.skills[0].usageEvidence,'UNVERIFIED');
});
test('unreadable skill is UNVERIFIED instead of crashing',{skip:process.platform==='win32'},()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-unreadable-skill-'));
  try {
    fs.mkdirSync(path.join(dir,'skill'),{recursive:true});
    fs.writeFileSync(path.join(dir,'skill','SKILL.md'),'# Private fixture\n');
    fs.chmodSync(path.join(dir,'skill','SKILL.md'),0);
    fs.writeFileSync(path.join(dir,'agent.yaml'),'skills:\n  - path: skill/SKILL.md\n');
    const result=spawnSync(process.execPath,[script,'--out','reports/audit'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(result.error, undefined, result.error?.message);
    assert.equal(result.status,0);
    const report=JSON.parse(fs.readFileSync(path.join(dir,'reports/audit.json'),'utf8'));
    assert.ok(report.findings.some(x=>x.code==='SKILL_UNREADABLE'&&x.status==='UNVERIFIED'));
    assert.equal(report.aiReview.recommendation,'CONDITIONAL_REVIEW');
  } finally {fs.rmSync(dir,{recursive:true,force:true});}
});
test('missing skill fails closed',()=>{
  const r=run({'agent.yaml':'skills:\n  - path: absent/SKILL.md\n'});
  assert.equal(r.status,1);assert.equal(r.report.findings[0].code,'SKILL_MISSING');
});
test('traversal skill path is rejected',()=>{
  const r=run({'agent.yaml':'skills:\n  - path: ../other/SKILL.md\n'});
  assert.equal(r.status,1);assert.equal(r.report.findings[0].code,'UNSAFE_SKILL_PATH');
});
test('unrecognised manifest cannot claim success',()=>{
  const r=run({'agent.yaml':'skills: [foo]\n'});
  assert.equal(r.status,0);assert.equal(r.report.findings[0].status,'UNVERIFIED');
  assert.equal(r.report.aiReview.recommendation,'CONDITIONAL_REVIEW');
});
test('baseline change suggests review but does not invoke model',()=>{
  const files={'agent.yaml':'skills:\n  - path: skill/SKILL.md\n','skill/SKILL.md':'# Changed\n','old.json':JSON.stringify({skills:[{path:'skill/SKILL.md',sha256:'0000000000000000000000000000000000000000000000000000000000000000'}]})};
  const r=run(files,['--baseline','old.json']);
  assert.equal(r.status,0);assert.deepEqual(r.report.changedSkills,['skill/SKILL.md']);
  assert.equal(r.report.aiReview.requested,false);
  assert.equal(r.report.policy.autoModelInvocation,false);
});
test('baseline skill removed from current manifest is reported as changed',()=>{
  const currentSha=crypto.createHash('sha256').update('# Current\n').digest('hex');
  const files={
    'agent.yaml':'skills:\n  - path: current/SKILL.md\n',
    'current/SKILL.md':'# Current\n',
    'old.json':JSON.stringify({skills:[
      {path:'current/SKILL.md',sha256:currentSha},
      {path:'removed/SKILL.md',sha256:'0000000000000000000000000000000000000000000000000000000000000000'}
    ]})
  };
  const r=run(files,['--baseline','old.json']);
  assert.equal(r.status,0);
  assert.deepEqual(r.report.changedSkills,['removed/SKILL.md']);
  assert.equal(r.report.aiReview.recommendation,'CONDITIONAL_REVIEW');
});
test('invalid baseline is unverified',()=>{
  const r=run({'old.json':'{invalid'},['--baseline','old.json']);
  assert.equal(r.report.findings[1].code,'BASELINE_INVALID');
});
test('unsafe output is rejected',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-path-'));
  try{
    const r=spawnSync(process.execPath,[script,'--out','../escape'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(r.error, undefined, r.error?.message);
    assert.equal(r.status,2);assert.match(r.stderr,/Unsafe output path/);
  } finally{fs.rmSync(dir,{recursive:true,force:true});}
});

test('malformed baseline skills yields UNVERIFIED instead of crashing',()=>{
  const r=run({'old.json':JSON.stringify({skills:{not:'an array'}})},['--baseline','old.json']);
  assert.equal(r.status,0);
  assert.ok(r.report.findings.some(x=>x.code==='BASELINE_SCHEMA_INVALID'));
});

test('malformed baseline entry yields UNVERIFIED instead of crashing',()=>{
  const r=run({'old.json':JSON.stringify({skills:[null]})},['--baseline','old.json']);
  assert.equal(r.status,0);
  assert.ok(r.report.findings.some(x=>x.code==='BASELINE_SCHEMA_INVALID'));
});

test('symlinked manifest outside root is blocked',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-manifest-link-'));
  const outside=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-manifest-outside-'));
  try {
    fs.writeFileSync(path.join(outside,'agent.yaml'),'skills:\n  - path: outside/SKILL.md\n');
    fs.symlinkSync(path.join(outside,'agent.yaml'),path.join(dir,'agent.yaml'));
    const result=spawnSync(process.execPath,[script,'--out','reports/audit'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(result.status,1);
    const report=JSON.parse(fs.readFileSync(path.join(dir,'reports/audit.json'),'utf8'));
    assert.ok(report.findings.some(x=>x.code==='MANIFEST_SYMLINK_ESCAPE'));
  } finally {fs.rmSync(dir,{recursive:true,force:true});fs.rmSync(outside,{recursive:true,force:true});}
});

test('symlinked baseline outside root is blocked',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-baseline-link-'));
  const outside=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-baseline-outside-'));
  try {
    fs.writeFileSync(path.join(outside,'baseline.json'),JSON.stringify({skills:[]}));
    fs.symlinkSync(path.join(outside,'baseline.json'),path.join(dir,'baseline.json'));
    const result=spawnSync(process.execPath,[script,'--baseline','baseline.json','--out','reports/audit'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(result.status,1);
    const report=JSON.parse(fs.readFileSync(path.join(dir,'reports/audit.json'),'utf8'));
    assert.ok(report.findings.some(x=>x.code==='BASELINE_SYMLINK_ESCAPE'));
  } finally {fs.rmSync(dir,{recursive:true,force:true});fs.rmSync(outside,{recursive:true,force:true});}
});

test('symlinked output directory outside root is rejected without writing outside',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-output-link-'));
  const outside=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-output-outside-'));
  try {
    fs.symlinkSync(outside,path.join(dir,'reports'),'dir');
    const result=spawnSync(process.execPath,[script,'--out','reports/audit'],{cwd:dir,encoding:'utf8',timeout:5000});
    assert.equal(result.status,2);
    assert.match(result.stderr,/Unsafe output path/);
    assert.equal(fs.existsSync(path.join(outside,'audit.json')),false);
    assert.equal(fs.existsSync(path.join(outside,'audit.md')),false);
  } finally {fs.rmSync(dir,{recursive:true,force:true});fs.rmSync(outside,{recursive:true,force:true});}
});

test('symlinked skill outside root is blocked',()=>{
  const dir=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-symlink-'));
  const outside=fs.mkdtempSync(path.join(os.tmpdir(),'skill-audit-outside-'));
  try {
    fs.mkdirSync(path.join(dir,'skills'),{recursive:true});
    fs.writeFileSync(path.join(outside,'SKILL.md'),'# Outside');
    fs.symlinkSync(path.join(outside,'SKILL.md'),path.join(dir,'skills','SKILL.md'));
    fs.writeFileSync(path.join(dir,'agent.yaml'),'skills:\n  - path: skills/SKILL.md\n');
    const result=spawnSync(process.execPath,[script,'--out','reports/audit'],{cwd:dir,encoding:'utf8'});
    assert.equal(result.status,1);
    const report=JSON.parse(fs.readFileSync(path.join(dir,'reports/audit.json'),'utf8'));
    assert.ok(report.findings.some(x=>x.code==='SKILL_SYMLINK_ESCAPE'));
  } finally {fs.rmSync(dir,{recursive:true,force:true});fs.rmSync(outside,{recursive:true,force:true});}
});
