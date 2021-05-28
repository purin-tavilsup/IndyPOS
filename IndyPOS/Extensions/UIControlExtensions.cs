﻿using System;
using System.Windows.Forms;

namespace IndyPOS.Extensions
{
    /// <summary>
	/// Extension for updating Control's properties
	/// </summary>
	public static class UIControlExtensions
    {
        /// <summary>
        /// Update Control's properties
        /// </summary>
        /// <param name="control"></param>
        /// <param name="code"></param>
        static public void UIThread(this Control control, Action code)
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(code);
            }
            else
            {
                code.Invoke();
            }
        }
    }
}
