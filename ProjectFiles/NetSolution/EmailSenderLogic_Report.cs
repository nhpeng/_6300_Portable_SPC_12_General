/* 
Class: EmailSenderLogic_Report
This class extends BaseNetLogic and handles the logic for sending emails, including retry mechanisms and validation.

// Variables:
// emailStatus: A variable indicating the status of email sending.
// maxDelay: A variable representing the maximum delay before retrying to send an email.
// retryPeriodicTask: A periodic task for retrying to send emails.
// smtpClient: An SMTP client for sending emails.
// senderAddress: The email address of the sender.
// senderPassword: The password for the sender’s email account.
// smtpHostname: The hostname of the SMTP server.
// smtpPort: The port of the SMTP server.
// enableSSL: A boolean indicating whether SSL is enabled for the SMTP client.*/

// Methods:
// Start(): Initializes the email sending status and delay before retry variables. Sets up an event handler for changes in the delay variable.
// RestartPeriodicTask(object sender, VariableChangeEventArgs e): Restarts the periodic task for sending queued messages if the delay is valid (at least 10 seconds).
// SendEmail(string mailToAddress, string mailSubject, string mailBody): Validates SMTP parameters and email content, then sends the email. If sending fails, it retries based on the configured delay.
// CreateEmailMessage(MailAddress fromAddress, MailAddress toAddress, string mailBody, string mailSubject): Creates an email message with the specified parameters and attaches any specified attachments.
// TrySendEmail(MailMessageWithRetries message): Attempts to send the email and handles retries if sending fails.
// SendQueuedMessage(PeriodicTask task): Sends queued messages periodically.
// As is where is SClark NHP.



#region Using directives
using System.Net.Mail;
using System.Net;
using FTOptix.Core;
using UAManagedCore;
using FTOptix.NetLogic;
using System.Collections.Generic;
using FTOptix.SerialPort;
using FTOptix.System;
using FTOptix.EventLogger;
using FTOptix.UI;
using FTOptix.WebUI;
using FTOptix.OPCUAServer;
using FTOptix.OPCUAClient;
using FTOptix.S7TiaProfinet;
using FTOptix.ODBCStore;
#endregion

public class EmailSenderLogic_Report : BaseNetLogic
{
    public override void Start()
    {
        ValidateCertificate();
        emailStatus = GetVariableValue("EmailSendingStatus");
        maxDelay = GetVariableValue("DelayBeforeRetry");
        maxDelay.VariableChange += RestartPeriodicTask;
    }

    private void RestartPeriodicTask(object sender, VariableChangeEventArgs e)
    {
        if (e.NewValue < 10000 || e.NewValue == null)
        {
            Log.Warning("EmailSenderLogic", "Minimum delay before retrying should be 10 seconds");
            return;
        }

        retryPeriodicTask?.Cancel();
        retryPeriodicTask = new PeriodicTask(SendQueuedMessage, e.NewValue, LogicObject);
        retryPeriodicTask.Start();
    }

    [ExportMethod]
    public void SendEmail(string mailToAddress, string mailSubject, string mailBody)
    {
        if (!InitializeAndValidateSMTPParameters())
            return;

        if (!ValidateEmail(mailToAddress, mailSubject, mailBody))
            return;

        var fromAddress = new MailAddress(senderAddress, "From");
        var toAddress = new MailAddress(mailToAddress, "To");

        if (retryPeriodicTask == null)
        {
            var delayBeforeRetry = GetVariableValue("DelayBeforeRetry").Value;
            if (delayBeforeRetry >= 10000)
            {
                retryPeriodicTask = new PeriodicTask(SendQueuedMessage, delayBeforeRetry, LogicObject);
                retryPeriodicTask.Start();
            }
        }

        smtpClient = new SmtpClient
        {
            Host = smtpHostname,
            Port = smtpPort,
            EnableSsl = enableSSL,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, senderPassword)
        };
        var message = CreateEmailMessage(fromAddress, toAddress, mailBody, mailSubject);
        TrySendEmail(message);
    }

    private MailMessageWithRetries CreateEmailMessage(MailAddress fromAddress, MailAddress toAddress, string mailBody, string mailSubject)
    {
        var mailMessage = new MailMessageWithRetries(fromAddress, toAddress)
        {
            Body = mailBody,
            Subject = mailSubject,
            BodyEncoding = System.Text.Encoding.UTF8,
        };

        var attachment = GetVariableValue("Attachment").Value;
        if (!string.IsNullOrEmpty(attachment))
        {
            var attachmentUri = new ResourceUri(attachment);
            mailMessage.Attachments.Add(new Attachment(attachmentUri.Uri));
        }

        mailMessage.ReplyToList.Add(toAddress);
        return mailMessage;
    }

    private void TrySendEmail(MailMessageWithRetries message)
    {
        if (!CanRetrySendingMessage(message))
            return;

        using (message)
        {
            try
            {
                message.AttemptNumber++;
                Log.Info("EmailSender", $"Sending Email... ");
                smtpClient.Send(message);

                emailStatus.Value = true;
                Log.Info("EmailSenderLogic", "Email sent successfully");
            }
            catch (SmtpException e)
            {
                emailStatus.Value = false;
                Log.Error("EmailSenderLogic", $"Email failed to send: {e.StatusCode} {e.Message}");

                if (CanRetrySendingMessage(message))
                    EnqueueFailedMessage(message);
            }
        }
    }

    private void SendQueuedMessage(PeriodicTask task)
    {
        if (failedMessagesQueue.Count == 0 || task.IsCancellationRequested)
            return;

        var message = failedMessagesQueue.Pop();

        if (CanRetrySendingMessage(message))
        {
            var retries = GetVariableValue("MaxRetriesOnFailure").Value;
            Log.Info($"Retry Sending email attempt {message.AttemptNumber} of {retries}");
            TrySendEmail(message);
        }
    }

    private void EnqueueFailedMessage(MailMessageWithRetries message)
    {
        failedMessagesQueue.Push(message);
    }

    private bool InitializeAndValidateSMTPParameters()
    {
        senderAddress = (string)GetVariableValue("SenderEmailAddress").Value;
        if (string.IsNullOrEmpty(senderAddress))
        {
            Log.Error("EmailSenderLogic", "Invalid Sender Email address");
            return false;
        }

        senderPassword = (string)GetVariableValue("SenderEmailPassword").Value;
        if (string.IsNullOrEmpty(senderPassword))
        {
            Log.Error("EmailSenderLogic", "Invalid sender password");
            return false;
        }

        smtpHostname = (string)GetVariableValue("SMTPHostname").Value;
        if (string.IsNullOrEmpty(smtpHostname))
        {
            Log.Error("EmailSenderLogic", "Invalid SMTP hostname");
            return false;
        }

        smtpPort = (int)GetVariableValue("SMTPPort").Value;
        enableSSL = (bool)GetVariableValue("EnableSSL").Value;

        return true;
    }

    private bool CanRetrySendingMessage(MailMessageWithRetries message)
    {
        var maxRetries = GetVariableValue("MaxRetriesOnFailure").Value;
        return maxRetries >= 0 && message.AttemptNumber <= maxRetries;
    }

    private class MailMessageWithRetries : MailMessage
    {
        public MailMessageWithRetries(MailAddress fromAddress, MailAddress toAddress)
            : base(fromAddress, toAddress)
        {

        }

        public int AttemptNumber { get; set; } = 0;
    }

    private IUAVariable GetVariableValue(string variableName)
    {
        var variable = LogicObject.GetVariable(variableName);
        if (variable == null)
        {
            Log.Error($"{variableName} not found");
            return null;
        }
        return variable;
    }

    private bool ValidateEmail(string recieverEmail, string emailSubject, string emailBody)
    {
        if (string.IsNullOrEmpty(emailSubject))
        {
            Log.Error("EmailSenderLogic", "Email subject is empty or malformed");
            return false;
        }

        if (string.IsNullOrEmpty(emailBody))
        {
            Log.Error("EmailSenderLogic", "Email body is empty or malformed");
            return false;
        }

        if (string.IsNullOrEmpty(recieverEmail))
        {
            Log.Error("EmailSenderLogic", "RecieverEmail is empty or null");
            return false;
        }
        return true;
    }

    private void ValidateCertificate()
    {
        if (System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            ServicePointManager.ServerCertificateValidationCallback = (_, __, ___, ____) => { return true; };
    }

    private string senderAddress;
    private string senderPassword;
    private string smtpHostname;
    private int smtpPort;
    private bool enableSSL;

    private SmtpClient smtpClient;
    private PeriodicTask retryPeriodicTask;
    private IUAVariable maxDelay;
    private IUAVariable emailStatus;
    private readonly Stack<MailMessageWithRetries> failedMessagesQueue = new Stack<MailMessageWithRetries>();
}
