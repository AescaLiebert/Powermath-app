# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](COMMIT_CONVENTION.md).

---

## [Unreleased]

### Added
- {New features — from `feat()` commits}

### Fixed
- {Bug fixes — from `fix()` commits}

### Changed
- {Refactors and behavior changes — from `refactor()` commits}

### Polished
- {Game feel / juice — from `juice()` commits}

### Removed
- {Deprecated features or dead code removed}


---

## Auto-Generation

### From Git Log (Manual)
```bash
# Generate raw changelog from conventional commits
git log --pretty=format:"- %s (%h)" --no-merges --since="{last-release-date}"
```

### Recommended Tools
| Tool | How |
|------|-----|
| [conventional-changelog](https://github.com/conventional-changelog/conventional-changelog) | `npx conventional-changelog -p angular -i CHANGELOG.md -s` |
| [git-cliff](https://github.com/orhun/git-cliff) | `git-cliff -o CHANGELOG.md` |
| [standard-version](https://github.com/conventional-changelog/standard-version) | `npx standard-version` |


