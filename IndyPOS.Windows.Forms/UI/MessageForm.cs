#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Windows.Forms.UI;

[ExcludeFromCodeCoverage]
public partial class MessageForm : Form
{
	private DialogResult _response = DialogResult.None;

	public MessageForm()
	{
		InitializeComponent();
	}
		
	public DialogResult Show(string message, 
							 string? caption = null, 
							 bool cancelButtonVisible = false, 
							 string? acceptButtonText = null, 
							 string? cancelButtonText = null)
	{
		if (caption is not null)
			CaptionLabel.Text = caption;

		if (acceptButtonText is not null)
			AcceptButton.Text = acceptButtonText;

		if (cancelButtonText is not null)
			CancelButton.Text = cancelButtonText;

		MessageTextBox.Texts = message;
		CancelButton.Visible = cancelButtonVisible;
		AcceptButton.Select();

		ShowDialog();

		return _response;
	}

	private void AcceptButton_Click(object sender, EventArgs e)
	{
		_response = DialogResult.OK;

		Close();
	}

	private void CancelButton_Click(object sender, EventArgs e)
	{
		_response = DialogResult.Cancel;

		Close();
	}
}