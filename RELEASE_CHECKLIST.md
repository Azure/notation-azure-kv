# Release Checklist

## Overview

This document describes the checklist to publish a release via GitHub workflow.

## Release Process
1. Determine a [SemVer2](https://semver.org/)-valid version prefixed with the letter `v` for release. For example, `version="v1.0.0-rc.1"`.
2. Submit a PR to update the `version` in `README.md` and wait approval for merging.
3. Be on the main branch connected to the actual repository (not a fork) and `git pull`.  Ensure `git log -1` shows the latest commit on the main branch.
4. Create a tag `git tag -am $version $version`
5. `git tag` and ensure the name in the list added looks correct, then push the tag directly to the repository by `git push --follow-tags`.
6. Wait for the completion of the GitHub action [release-github](https://github.com/Azure/notation-azure-kv/actions/workflows/release.yml).
7. Check the new draft release, revise the release description, and publish the release.
8. Announce the release in the community.
