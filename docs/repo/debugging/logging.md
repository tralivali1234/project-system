# Logging

The project system code logs information to a custom Output Window pane either
while debugging or when a certain environment variable is set.

## Enabling project system logs

Setting the `PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED` environment variable to
`1` enables project system logging.

This environment variable is set automatically when launching the
`ProjectSystemSetup` project within Visual Studio, via its
`launchSettings.json` file.

To enable this logging in other situations you may, for example:

1. Start a Developer Command Prompt
2. Run: `set PROJECTSYSTEM_PROJECTOUTPUTPANEENABLED=1`
3. Run: `devenv`
4. Open a solution
5. Use "View.Output Window"
6. Select the pane titled "Project" from the dropdown