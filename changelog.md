# Changelog #
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## Unreleased ##
 
## Known Issues ##
- Solution console: The color of the input text is not sometimes working properly

## [1.2.2] - 2018-12-29 ##

### Fixed ###
- Do not ovewrite file names applied to subfolders as well


## [1.2.1] - 2018-12-11 ##

### Fixed ###
 - FileUtils (used in Apply Template) - don't overwrite files now compares exactly the file name and is case insensitive
 - FileUtils (used in Apply Template) - keeps the case of file names during copy from template
 - Fixed the crash when a template was applied to solution with solution items virtual project (crashed)
 

## [1.2.0] - 2018-11-10 ##
### Added ###
- Checklists functionality
- Version info dialog

### Fixed ###
- Problems with console (BackgroundWorker) when restarting the process
- Cancel in Choose template dialog did not stop the operation, but the first one from the list has been applied
- Choose template and Console windows are movable now
- Some corrections in readme.md


## [1.1.0] - 2018-11-05 ##
### Added ###
- Solution console

## [1.0.1] - 2018-11-04 ##
### Fixed ###
- readme.md image tags (case for file names and the alt texts)
- version 1.0.0 link within this changelog

## [1.0.0] - 2018-11-04 ##
### Added ###
- Initial release
- Solution (Project) Templates functionality

[1.2.2]: https://github.com/adamecr/RadProjectsExtension/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/adamecr/RadProjectsExtension/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/adamecr/RadProjectsExtension/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/adamecr/RadProjectsExtension/compare/v1.0.1...v1.1.0
[1.0.1]: https://github.com/adamecr/RadProjectsExtension/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/adamecr/RadProjectsExtension/releases/tag/v1.0.0
