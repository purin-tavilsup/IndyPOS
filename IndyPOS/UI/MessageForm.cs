using IndyPOS.Extensions;
using System;
using System.Windows.Forms;

namespace IndyPOS.UI
{
	public partial class MessageForm : Form
	{
		private DialogResult _response = DialogResult.None;

		public MessageForm()
		{
			InitializeComponent();
		}
		
		public DialogResult Show(string message, string caption = null, bool cancelButtonVisible = false, string acceptButtonText = null, string cancelButtonText = null)
		{
			if (caption.HasValue())
				CaptionLabel.Text = caption;

			if (acceptButtonText.HasValue())
				AcceptButton.Text = acceptButtonText;

			if (cancelButtonText.HasValue())
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
}
