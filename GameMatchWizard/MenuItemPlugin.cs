﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unbroken.LaunchBox.Plugins;

namespace GameMatchWizard
{
	internal class MenuItemPlugin : ISystemMenuItemPlugin
	{
		public string Caption
		{
			get
			{
				return "GameMatchWizard";
			}
		}

		public System.Drawing.Image IconImage
		{
			get
			{
				return SystemIcons.Exclamation.ToBitmap();
			}
		}

		public bool ShowInLaunchBox
		{
			get
			{
				return true;
			}
		}


		public bool ShowInBigBox
		{
			get
			{
				return false;
			}
		}


		public bool AllowInBigBoxWhenLocked
		{
			get
			{
				return false;
			}
		}

		public void OnSelected()
		{
			var x = new Form2();
			x.ShowDialog();

		}
	}
}
