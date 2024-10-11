#region Using directives
using System;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.Core;
using FTOptix.UI;
using FTOptix.Alarm;
#endregion

public class LoginButtonLogic : BaseNetLogic
{
    // This method is called when the logic starts
    public override void Start()
    {
        // Retrieve the ComboBox for the username input
        ComboBox comboBox = Owner.Owner.Get<ComboBox>("Username");

        // Get the current authentication mode
        var authenticationMode = GetAuthenticationMode();

        // Set the ComboBox mode based on the authentication mode
        if (authenticationMode == AuthenticationMode.ModelOnly)
        {
            comboBox.Mode = ComboBoxMode.Normal;
        }
        else
        {
            comboBox.Mode = ComboBoxMode.Editable;
        }
    }

    // This method is called when the logic stops
    public override void Stop()
    {
        // Add any cleanup code here if necessary
    }

    // Placeholder method to get the current authentication mode
    private AuthenticationMode GetAuthenticationMode()
    {
        // Implement the logic to retrieve the current authentication mode
        // This is a placeholder implementation and should be replaced with actual logic
        return AuthenticationMode.ModelOnly; // Replace with actual logic to get the authentication mode
    }

    // Method to perform the login operation
    [ExportMethod]
    public void PerformLogin(string username, string password)
    {
        // Retrieve the alias for the Users node
        var usersAlias = LogicObject.GetAlias("Users");
        if (usersAlias == null || usersAlias.NodeId == NodeId.Empty)
        {
            Log.Error("LoginButtonLogic", "Missing Users alias");
            return;
        }

        // Retrieve the alias for the PasswordExpiredDialogType
        var passwordExpiredDialogType = LogicObject.GetAlias("PasswordExpiredDialogType") as DialogType;
        if (passwordExpiredDialogType == null)
        {
            Log.Error("LoginButtonLogic", "Missing PasswordExpiredDialogType alias");
            return;
        }

        // Disable the login button to prevent multiple clicks
        Button loginButton = (Button)Owner;
        loginButton.Enabled = false;

        try
        {
            // Attempt to login with the provided username and password
            var loginResult = Session.Login(username, password);

            // Check if the password has expired
            if (loginResult.ResultCode == ChangeUserResultCode.PasswordExpired)
            {
                // Retrieve the user and open the password expired dialog
                var user = usersAlias.Get<User>(username);
                var ownerButton = (Button)Owner;
                ownerButton.OpenDialog(passwordExpiredDialogType, user.NodeId);
            }
            else if (loginResult.ResultCode != ChangeUserResultCode.Success)
            {
                // Log an error if authentication failed
                Log.Error("LoginButtonLogic", "Authentication failed");
            }
            else
            {
                // Retrieve the output message label and logic
                var outputMessageLabel = Owner.Owner.GetObject("LoginFormOutputMessage");
                if (outputMessageLabel != null)
                {
                    var outputMessageLogic = outputMessageLabel.GetObject("LoginFormOutputMessageLogic");
                    if (outputMessageLogic != null)
                    {
                        // Set the output message based on the login result
                        outputMessageLogic.ExecuteMethod("SetOutputMessage", new object[] { (int)loginResult.ResultCode });
                    }
                    else
                    {
                        Log.Error("LoginButtonLogic", "LoginFormOutputMessageLogic not found");
                    }
                }
                else
                {
                    Log.Error("LoginButtonLogic", "LoginFormOutputMessage not found");
                }
            }
        }
        catch (Exception e)
        {
            // Log any exceptions that occur during the login process
            Log.Error("LoginButtonLogic", e.Message);
        }
        finally
        {
            // Re-enable the login button
            loginButton.Enabled = true;
        }
    }
}
