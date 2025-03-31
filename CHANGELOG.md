# Changelog
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.3.1] - 2025-03-31
### Fixed
- Fix root menu position not set correctly when displayed for the first time

## [1.3.0] - 2025-03-31
### Added
- Dropdown items now clear all potentially bindings before being returned to the pool (only Unity 2023.2 and later).
### Changed
- Sub menus are now calculating their position on demand, allowing for changes of any menu's size to be correctly reflected in its positioning.
- BlockingBehavior `None` has been renamed to `Default`.
### Removed
- BlockingBehavior `All` has been removed.

## [1.2.0] - 2024-12-20
### Added
- Dropdowns opened with an `EventBase` argument will now add `.fab-dropdown-target--open` USS class to the events target.
- The blocking behavior of pointer interaction outside an opened dropdown can now be configured through the dedicated `BlockingLayer` element.
### Changed
- Default menu border radius is now 0.
### Fixed
- Fix USS icon path not working in samples.

## [1.1.0] - 2024-09-04
### Added
- Dropdown menus are now auto positioning to fit the screen.
- Dropdown styles are now easier to customize through USS variables.
- Positioning of sub menus can now be styled through USS.
- Menus can now be further customized through the `setMenu` callback. 
- Default methods for creating dropdown items are now public.
- Dropdown open calls now have the option to pass the trigger event accessible as `eventInfo` to each menu action item. 
- Delay before a hovered item opens its sub menu can now be customized.
### Changed
- Dropdown navigation focusing, submit and cancel now works reliably using navigation events
- Closing the dropdown by clicking outside the dropdown menu now registers clicks on the underlying element.
- The root layer has been renamed from `#blocking-layer` to `.fab-dropdown`.
- The USS class name for dropdown menus has been changed from `.fab-dropdown__outer-container` to `.fab-dropdown__menu-container` 
- The USS class of menu items has been changed from `.fab-dropdown__item` to `fab-dropdown__menu-item`
- Child elements of menu items class' names have changed to the format `.fab-dropdown-menu-item__`
### Removed
- Remove VisualElement extensions.
### Fixed
- Fix hiding submenus with only hidden items

## [1.0.1] - 2024-05-14
### Fixed
- Fix deprecated call to `PropagationPhase.AtTarget`. 
- Fix menu item stays highlighted when submenu is open.

[Unreleased]: https://github.com/fabSchneider/fab.uitk-runtime-dropdown/compare/v1.3.1...HEAD
[1.3.1]: https://github.com/fabSchneider/fab.uitk-runtime-dropdown/releases/tag/v1.3.1
[1.3.0]: https://github.com/fabSchneider/fab.uitk-runtime-dropdown/releases/tag/v1.3.0
[1.2.0]: https://github.com/fabSchneider/fab.uitk-runtime-dropdown/releases/tag/v1.2.0
[1.1.0]: https://github.com/fabSchneider/fab.uitk-runtime-dropdown/releases/tag/v1.1.0
[1.0.1]: https://github.com/fabSchneider/fab.uitk-runtime-dropdown/releases/tag/v1.0.1
