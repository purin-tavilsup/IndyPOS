// IndyPOS.Domain pulls in Prism.Core, whose buildTransitive targets add a global
// "using Prism.Dialogs;" (for ImplicitUsings consumers). Prism.Dialogs also declares
// a DialogResult type, ambiguous with the WinForms one used throughout this project.
// Alias it project-wide so no bootstrapper file needs a per-file workaround.
global using DialogResult = System.Windows.Forms.DialogResult;
