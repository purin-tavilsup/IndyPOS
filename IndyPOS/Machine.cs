using IndyPOS.UI;

namespace IndyPOS
{
    public class Machine : IMachine
    {
        private MainForm _mainForm;

        public Machine(MainForm mainForm)
        {
            _mainForm = mainForm;
        }
        
        public void Launch()
        {
            StartUI();
        }

        private void StartUI()
        {
            System.Windows.Forms.Application.Run(_mainForm);

            _mainForm.BringToFront();
        }
    }
}
