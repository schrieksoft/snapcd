// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text;

namespace SnapCd.Server.Core.Services.Email;

public static class EmailTemplateHelper
{
    public static string GenerateEmailConfirmationEmail(string confirmationLink, string hostUrl)
    {
        return GenerateEmailTemplate(
            "Confirm Your Email",
            "Email Confirmation Required",
            @"
                <p style='margin: 0;'>
                    Thank you for registering with Snap CD. Please confirm your email address by clicking the button below:
                </p>",
            confirmationLink,
            "confirm email address",
            "If you didn't create an account with Snap CD, you can safely ignore this email.",
            hostUrl
        );
    }

    public static string GeneratePasswordResetEmail(string resetLink, string hostUrl)
    {
        var expirationTime = DateTime.UtcNow.AddHours(24).ToString("MMMM d, yyyy 'at' h:mm tt 'UTC'");

        return GenerateEmailTemplate(
            "Reset Your Password",
            "Password Reset Request",
            @"
                <p style='margin: 0 0 16px 0;'>
                    We received a request to reset the password for your Snap CD account.
                </p>
                <p style='margin: 0;'>
                    Click the button below to reset your password. If you didn't make this request, you can safely ignore this email.
                </p>",
            resetLink,
            "reset password",
            $"This password reset link will expire on <strong>{expirationTime}</strong>.<br/>For security reasons, this link can only be used once.",
            hostUrl
        );
    }

    public static string GenerateOrganizationInvitationEmail(string invitationLink, string organizationName, string inviterName, string inviterEmail, string hostUrl, int expirationDays = 30)
    {
        var expirationTime = DateTime.UtcNow.AddDays(expirationDays).ToString("MMMM d, yyyy");

        return GenerateEmailTemplate(
            $"{inviterName} invited you to {organizationName}",
            "You've Been Invited",
            $@"
                <p style='margin: 0 0 16px 0;'>
                    <strong>{inviterName}</strong> ({inviterEmail}) has invited you to join <strong>{organizationName}</strong> on Snap CD.
                </p>
                <p style='margin: 0 0 16px 0;'>
                    Click the button below to accept or decline this invitation.
                </p>
                <p style='margin: 0 0 8px 0; font-size: 13px; color: #9AA5B0;'>
                    <strong>Privacy Notice:</strong> This is the only email you will receive from SnapCD unless you accept this invitation and create an account.
                    Your email address will not be used for any other purpose.
                </p>",
            invitationLink,
            "accept or decline",
            $"This invitation will expire on <strong>{expirationTime}</strong> ({expirationDays} days).<br/>If you decline or ignore this invitation, your information will be automatically deleted.",
            hostUrl
        );
    }

    private static string GenerateEmailTemplate(
        string title,
        string heading,
        string bodyContent,
        string actionLink,
        string actionText,
        string footerNote,
        string hostUrl)
    {
        var sb = new StringBuilder();

        sb.Append(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>").Append(title).Append(@"</title>
    <!--[if mso]>
    <noscript>
        <xml>
            <o:OfficeDocumentSettings>
                <o:PixelsPerInch>96</o:PixelsPerInch>
            </o:OfficeDocumentSettings>
        </xml>
    </noscript>
    <![endif]-->
</head>
<body style='margin: 0; padding: 0; font-family: ""Geist Sans"", -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background-color: #000000;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0; background: #000000;'>
        <tr>
            <td align='center' style='padding: 40px 0;'>
                <table role='presentation' style='width: 600px; border-collapse: collapse; border: 0; border-spacing: 0; background: #171D24; border: 1px solid #262E37; box-shadow: 0 2px 4px rgba(0,0,0,0.35), 0 1px 2px rgba(0,0,0,0.2);'>
                    <!-- Header -->
                    <tr>
                        <td style='padding: 40px 40px 20px 40px; text-align: center; background: #0E1216;'>
                            <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0;'>
                                <tr>
                                    <td style='text-align: center;'>
                                        <h1 style='color: #E8ECEF; font-size: 24px; margin: 0; font-weight: 600; letter-spacing: -0.015em;'>Snap <span style='color: #E85D1A;'>CD</span></h1>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='color: #E8ECEF; font-size: 24px; margin: 0 0 24px 0; font-weight: 600; text-align: center;'>").Append(heading).Append(@"</h2>
                            
                            <div style='color: #9AA5B0; font-size: 16px; line-height: 1.6; margin-bottom: 32px; text-align: center;'>
                                ").Append(bodyContent).Append(@"
                            </div>
                            
                            <!-- CTA Button -->
                            <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0; margin: 32px 0;'>
                                <tr>
                                    <td align='center'>
                                        <a href='").Append(actionLink).Append(@"' 
                                           style='display: inline-block; padding: 8px 24px; background: #E85D1A; color: #12171D; text-decoration: none; border-radius: 0; font-weight: 500; font-size: 14px; text-transform: lowercase;'>
                                            ").Append(actionText).Append(@"
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <!-- Alternative Link -->
                            <div style='background: #0E1216; border: 1px solid #262E37; padding: 20px; margin: 24px 0;'>
                                <p style='color: #9AA5B0; font-size: 14px; margin: 0 0 8px 0; text-align: center;'>
                                    Or copy and paste this link into your browser:
                                </p>
                                <p style='color: #E85D1A; font-size: 14px; word-break: break-all; margin: 0; text-align: center;'>
                                    <a href='").Append(actionLink).Append(@"' style='color: #E85D1A; text-decoration: none;'>").Append(actionLink).Append(@"</a>
                                </p>
                            </div>
                            
                            <!-- Footer Note -->
                            <div style='border-top: 1px solid #262E37; margin-top: 32px; padding-top: 24px;'>
                                <p style='color: #9AA5B0; font-size: 14px; line-height: 1.5; margin: 0; text-align: center;'>
                                    ").Append(footerNote).Append(@"
                                </p>
                            </div>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 24px 40px; background: #0E1216; border-top: 1px solid #262E37;'>
                            <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0;'>
                                <tr>
                                    <td style='text-align: center;'>
                                        <p style='color: #9AA5B0; font-size: 13px; margin: 0 0 4px 0;'>
                                            © ").Append(DateTime.UtcNow.Year).Append(@" Snap CD. All rights reserved.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

        return sb.ToString();
    }

    public static string GetPlainTextVersion(string subject, string actionLink, string message)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Snap CD");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine(subject);
        sb.AppendLine();
        sb.AppendLine(message);
        sb.AppendLine();
        sb.AppendLine("Click the following link to continue:");
        sb.AppendLine(actionLink);
        sb.AppendLine();
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine("© " + DateTime.UtcNow.Year + " Snap CD. All rights reserved.");

        return sb.ToString();
    }

    public static string GetOrganizationInvitationPlainText(string organizationName, string inviterName, string invitationLink)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Snap CD");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine("You've Been Invited");
        sb.AppendLine();
        sb.AppendLine($"{inviterName} has invited you to join {organizationName}.");
        sb.AppendLine();
        sb.AppendLine("Click the following link to accept or decline the invitation:");
        sb.AppendLine(invitationLink);
        sb.AppendLine();
        sb.AppendLine("This invitation will expire in 7 days.");
        sb.AppendLine("If you decline or ignore this invitation, your information will be automatically deleted.");
        sb.AppendLine();
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine("© " + DateTime.UtcNow.Year + " Snap CD. All rights reserved.");

        return sb.ToString();
    }

    public static string GeneratePasswordResetCodeEmail(string resetCode)
    {
        return $@"
            <p>A password reset has been requested for your Snap CD account.</p>
            <p>Your password reset code is: <strong style='font-size: 18px; color: #E8ECEF;'>{resetCode}</strong></p>
            <p>Please enter this code to reset your password.</p>";
    }

    public static string GetPasswordResetCodePlainText(string resetCode)
    {
        return $"A password reset has been requested for your Snap CD account.\n\nYour password reset code is: {resetCode}\n\nPlease enter this code to reset your password.";
    }

    public static string GetContactFormSubject(string fromName, string subject)
    {
        return $"Contact Form: {subject} (from {fromName})";
    }

    public static string GenerateContactFormEmail(string fromName, string fromEmail, string subject, string message, string hostUrl)
    {
        return GenerateContactFormTemplate(
            $"Contact Form Submission - {subject}",
            "New Contact Form Message",
            $@"
                <p style='margin: 0 0 16px 0;'>
                    You have received a new message from the contact form on Snap CD.
                </p>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 16px;'>
                    <tr>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #E8ECEF; font-weight: 600; width: 120px;'>From:</td>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #9AA5B0;'>{System.Net.WebUtility.HtmlEncode(fromName)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #E8ECEF; font-weight: 600;'>Email:</td>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #9AA5B0;'><a href='mailto:{System.Net.WebUtility.HtmlEncode(fromEmail)}' style='color: #E85D1A;'>{System.Net.WebUtility.HtmlEncode(fromEmail)}</a></td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #E8ECEF; font-weight: 600;'>Subject:</td>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #9AA5B0;'>{System.Net.WebUtility.HtmlEncode(subject)}</td>
                    </tr>
                </table>
                <div style='background: #0E1216; border: 1px solid #262E37; padding: 16px; margin-top: 16px;'>
                    <p style='margin: 0 0 8px 0; color: #E8ECEF; font-weight: 600;'>Message:</p>
                    <p style='margin: 0; color: #9AA5B0; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
                </div>",
            hostUrl
        );
    }

    public static string GetContactFormPlainText(string fromName, string fromEmail, string subject, string message)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Snap CD - Contact Form Submission");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"From: {fromName}");
        sb.AppendLine($"Email: {fromEmail}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine();
        sb.AppendLine("Message:");
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine(message);
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine();
        sb.AppendLine("© " + DateTime.UtcNow.Year + " Snap CD. All rights reserved.");

        return sb.ToString();
    }

    public static string GetContactFormConfirmationSubject(string subject)
    {
        return $"We received your message: {subject}";
    }

    public static string GenerateContactFormConfirmationEmail(string toName, string subject, string message, string hostUrl)
    {
        return GenerateContactFormTemplate(
            "Message Received - Snap CD",
            "We Received Your Message",
            $@"
                <p style='margin: 0 0 16px 0;'>
                    Hi {System.Net.WebUtility.HtmlEncode(toName)},
                </p>
                <p style='margin: 0 0 16px 0;'>
                    Thank you for contacting us. We have received your message and will respond soon!
                </p>
                <p style='margin: 0 0 8px 0; font-weight: 600;'>Your message:</p>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 16px;'>
                    <tr>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #E8ECEF; font-weight: 600; width: 120px;'>Subject:</td>
                        <td style='padding: 8px 0; border-bottom: 1px solid #262E37; color: #9AA5B0;'>{System.Net.WebUtility.HtmlEncode(subject)}</td>
                    </tr>
                </table>
                <div style='background: #0E1216; border: 1px solid #262E37; padding: 16px; margin-top: 16px;'>
                    <p style='margin: 0; color: #9AA5B0; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
                </div>",
            hostUrl
        );
    }

    public static string GetContactFormConfirmationPlainText(string toName, string subject, string message)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Snap CD - Message Received");
        sb.AppendLine("=".PadRight(50, '='));
        sb.AppendLine();
        sb.AppendLine($"Hi {toName},");
        sb.AppendLine();
        sb.AppendLine("Thank you for contacting us. We have received your message and will respond soon!");
        sb.AppendLine();
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine();
        sb.AppendLine("Your message:");
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine(message);
        sb.AppendLine("-".PadRight(50, '-'));
        sb.AppendLine();
        sb.AppendLine("© " + DateTime.UtcNow.Year + " Snap CD. All rights reserved.");

        return sb.ToString();
    }

    private static string GenerateContactFormTemplate(
        string title,
        string heading,
        string bodyContent,
        string hostUrl)
    {
        var sb = new StringBuilder();

        sb.Append(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>").Append(title).Append(@"</title>
    <!--[if mso]>
    <noscript>
        <xml>
            <o:OfficeDocumentSettings>
                <o:PixelsPerInch>96</o:PixelsPerInch>
            </o:OfficeDocumentSettings>
        </xml>
    </noscript>
    <![endif]-->
</head>
<body style='margin: 0; padding: 0; font-family: ""Geist Sans"", -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; background-color: #000000;'>
    <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0; background: #000000;'>
        <tr>
            <td align='center' style='padding: 40px 0;'>
                <table role='presentation' style='width: 600px; border-collapse: collapse; border: 0; border-spacing: 0; background: #171D24; border: 1px solid #262E37; box-shadow: 0 2px 4px rgba(0,0,0,0.35), 0 1px 2px rgba(0,0,0,0.2);'>
                    <!-- Header -->
                    <tr>
                        <td style='padding: 40px 40px 20px 40px; text-align: center; background: #0E1216;'>
                            <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0;'>
                                <tr>
                                    <td style='text-align: center;'>
                                        <h1 style='color: #E8ECEF; font-size: 24px; margin: 0; font-weight: 600; letter-spacing: -0.015em;'>Snap <span style='color: #E85D1A;'>CD</span></h1>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <h2 style='color: #E8ECEF; font-size: 24px; margin: 0 0 24px 0; font-weight: 600; text-align: center;'>").Append(heading).Append(@"</h2>

                            <div style='color: #9AA5B0; font-size: 16px; line-height: 1.6;'>
                                ").Append(bodyContent).Append(@"
                            </div>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='padding: 24px 40px; background: #0E1216; border-top: 1px solid #262E37;'>
                            <table role='presentation' style='width: 100%; border-collapse: collapse; border: 0; border-spacing: 0;'>
                                <tr>
                                    <td style='text-align: center;'>
                                        <p style='color: #9AA5B0; font-size: 13px; margin: 0 0 4px 0;'>
                                            © ").Append(DateTime.UtcNow.Year).Append(@" Snap CD. All rights reserved.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>");

        return sb.ToString();
    }
}