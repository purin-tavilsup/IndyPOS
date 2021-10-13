using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IndyPOS.Extensions;

namespace IndyPOS.UI
{
	public partial class MessageForm : Form
	{
		private DialogResult _response = DialogResult.None;

		public MessageForm()
		{
			InitializeComponent();
		}

		public DialogResult Show(string message)
		{
			MessageTextBox.Texts = message;
			CancelButton.Visible = true;
			AcceptButton.Select();

			ShowDialog();

			return _response;
		}

		public DialogResult Show(string message, bool cancelButtonVisible, string acceptButtonText = null, string cancelButtonText = null)
		{
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
