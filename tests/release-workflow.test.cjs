'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const workflow = fs.readFileSync(path.join(__dirname, '../.github/workflows/release.yml'), 'utf8');

test('v0.1.2 release only starts from a fixed manual dispatch on main', () => {
  assert.match(workflow, /workflow_dispatch:/);
  assert.doesNotMatch(workflow, /^\s+push:/m);
  assert.match(workflow, /group: windows-release-v0\.1\.2/);
  assert.match(workflow, /GITHUB_EVENT_NAME -ne 'workflow_dispatch'.*GITHUB_REF_NAME -ne 'main'/);
  assert.match(workflow, /DISPATCH_RELEASE_TAG -ne \$expectedTag/);
});

test('publisher creates or resumes a source-matched tag only after Windows gates', () => {
  const publisherStart = workflow.indexOf('  publish-prerelease:');
  const publisher = workflow.slice(publisherStart);
  assert.ok(publisherStart >= 0);
  assert.match(publisher, /needs: smoke-and-verify/);
  assert.match(publisher, /gh api --method POST "repos\/\$GITHUB_REPOSITORY\/git\/refs"/);
  assert.match(publisher, /Existing v0\.1\.2 tag matches the exact tested source SHA; continuing an interrupted release/);
  assert.match(publisher, /existing_tag_sha.*!=.*EXPECTED_SHA/);
  assert.match(publisher, /case "\$release_status" in[\s\S]*404\)[\s\S]*200\)/);
  assert.match(publisher, /A published v0\.1\.2 Release already exists; refusing to alter it/);
  assert.match(publisher, /-f "sha=\$EXPECTED_SHA"/);
  assert.match(publisher, /actual_tag_sha=.*resolve_tag_commit/);
  assert.match(publisher, /actual_tag_sha" != "\$EXPECTED_SHA/);
  assert.match(publisher, /gh release create "\$RELEASE_TAG" --verify-tag --draft --prerelease/);
  assert.doesNotMatch(publisher, /softprops\/action-gh-release/);
});

test('publisher resumes only the exact draft, validates every remote asset, then publishes', () => {
  const publisher = workflow.slice(workflow.indexOf('  publish-prerelease:'));
  const draftCreate = publisher.indexOf('gh release create "$RELEASE_TAG" --verify-tag --draft --prerelease');
  const draftCheck = publisher.indexOf("jq -r '.draft' <<< \"$release_json\"");
  const sourceFingerprint = publisher.indexOf('Release source SHA:');
  const manifestFingerprint = publisher.indexOf('Artifact manifest SHA-256:');
  const upload = publisher.indexOf('gh release upload "$RELEASE_TAG" "$local_path"');
  const remoteDownload = publisher.indexOf('gh release download "$RELEASE_TAG" --pattern "$name" --dir "$remote_dir"');
  const exactAssetCount = publisher.indexOf('Draft does not contain exactly the verified Windows release assets.');
  const finalHashCheck = publisher.lastIndexOf('Final draft asset SHA-256 mismatch:');
  const publish = publisher.indexOf('gh api --method PATCH "repos/$GITHUB_REPOSITORY/releases/$release_id"');

  assert.ok(draftCreate >= 0);
  assert.ok(draftCheck > draftCreate);
  assert.ok(sourceFingerprint > 0 && manifestFingerprint > sourceFingerprint);
  assert.ok(upload > manifestFingerprint);
  assert.ok(remoteDownload > upload);
  assert.ok(exactAssetCount > remoteDownload);
  assert.ok(finalHashCheck > exactAssetCount);
  assert.ok(publish > finalHashCheck);
  assert.match(publisher, /Existing draft artifact manifest differs from this exact package/);
  assert.match(publisher, /Existing draft asset differs from this exact artifact/);
  assert.match(publisher, /A published v0\.1\.2 Release already exists; refusing to alter it/);
  assert.match(publisher, /Published fully verified v0\.1\.2 prerelease/);
  assert.doesNotMatch(publisher, /continue-on-error|always\(\)/);
});
