using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Unbroken.LaunchBox.Plugins;

namespace GameMatchWizard
{

	public partial class Form2 : Form
	{
		GameMatcher gameMatcher;
		private IKeyboardEvents _keyboardEvents;
		bool handlerActive = false;
		public Form2()
		{
			InitializeComponent();
			GameMatcher.Init();
		}

		private void Form2_Load(object sender, EventArgs e)
		{
			Disable();
			
			_keyboardEvents = Hook.GlobalEvents();
			gameMatcher = new GameMatcher();
			
			foreach(var p in gameMatcher.plateformList)
			{
				comboBox1.Items.Add(p.name);
			}
		}

		private void GlobalHookKeyPress(object sender, KeyEventArgs e)
		{
			int keyval = -1;
			if (e.KeyValue>=96 && e.KeyValue <= 101) keyval = e.KeyValue - 96;
			if (e.KeyValue >= 48 && e.KeyValue <= 53) keyval = e.KeyValue - 48;


			if (keyval>=0)
			{
				e.Handled = true;
				if (keyval != 0)
				{
					var selectedGameText = gameMatcher.SelectSuspect(keyval);
					if(!String.IsNullOrWhiteSpace(selectedGameText)) listBox_change.Items.Add(selectedGameText);

				}
				if (gameMatcher.NextGame())
				{
					label_gamefile.Text = gameMatcher.selectedGame.Title;
					label_pos.Text = gameMatcher.positionInMissingGames.ToString() + " / " + gameMatcher.numberOfMissingGames.ToString();
					UpdateSuspect(gameMatcher.selectedSuspects);
				}
				else
				{
					if(handlerActive) _keyboardEvents.KeyUp -= GlobalHookKeyPress;
					groupBox1.Enabled= false;
				}
				
			}
			
		}


		private void button1_Click(object sender, EventArgs e)
		{
			if (gameMatcher.positionInMissingGames > 0 && handlerActive)
			{
				handlerActive= false;
				_keyboardEvents.KeyUp -= GlobalHookKeyPress;
			}
			listBox_change.Items.Clear();

			gameMatcher.SelectPlateform(comboBox1.Text);
			if(gameMatcher.positionInMissingGames > 0)
			{
				label_gamefile.Text = gameMatcher.selectedGame.Title;
				label_pos.Text = gameMatcher.positionInMissingGames.ToString() + " / " + gameMatcher.numberOfMissingGames.ToString();
				if (!handlerActive)
				{
					_keyboardEvents.KeyUp += GlobalHookKeyPress;
					handlerActive = true;
				}

				UpdateSuspect(gameMatcher.selectedSuspects);
				groupBox1.Enabled = true;

			}
			else
			{
				MessageBox.Show("No Games missing !");
				Disable();
			}

		}

		public void UpdateSuspect(List<Suspect> suspects)
		{
			Label[] label_suspect_Array = { label_suspect_1, label_suspect_2, label_suspect_3, label_suspect_4, label_suspect_5 };
			Label[] label_aka_Array = { label_aka_1, label_aka_2, label_aka_3, label_aka_4, label_aka_5 };
			foreach (var label_suspect in label_suspect_Array) label_suspect.Text = "";
			foreach (var label_aka in label_aka_Array) label_aka.Text = "";
			
			for(int i=0;i<suspects.Count;i++)
			{
				label_suspect_Array[i].Text = suspects[i].name;
				foreach (var aka in suspects.ElementAt(i).aka) label_aka_Array[i].Text += aka.Name + "\r\n";
			}
		}

		public void Disable()
		{
			Label[] label_suspect_Array = { label_suspect_1, label_suspect_2, label_suspect_3, label_suspect_4, label_suspect_5 };
			Label[] label_aka_Array = { label_aka_1, label_aka_2, label_aka_3, label_aka_4, label_aka_5 };
			foreach (var label_suspect in label_suspect_Array) label_suspect.Text = "";
			foreach (var label_aka in label_aka_Array) label_aka.Text = "";
			listBox_change.Items.Clear();
			groupBox1.Enabled= false;
			label_gamefile.Text = "";
			label_pos.Text = "";
			if (handlerActive)
			{
				handlerActive = false;
				_keyboardEvents.KeyUp -= GlobalHookKeyPress;
			}
		}

		private void btn_cancel_Click(object sender, EventArgs e)
		{
			gameMatcher.Unload();
			Disable();
		}

		private void btn_save_Click(object sender, EventArgs e)
		{
			gameMatcher.Save();
			Disable();
			MessageBox.Show("Done !");
			
		}
	}
}
