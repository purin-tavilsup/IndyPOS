using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Windows.Forms.UI.Login;

/// <summary>WinForms implementation of the change-password prompt seam.</summary>
public class ChangePasswordPrompt : IChangePasswordPrompt
{
    public Task<string?> RequestNewPasswordAsync()
    {
        using var form = new ChangePasswordForm();
        var result = form.ShowDialog();
        return Task.FromResult(result == System.Windows.Forms.DialogResult.OK ? form.NewPassword : null);
    }
}
