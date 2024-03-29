﻿using SuperAdventure.BLL;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SuperAdventure.UI
{
    public partial class SuperAdventure : Form
    {
        private const string PLAYER_DATA_FILE_NAME = "PlayerData.xml";

        private readonly Player _player;

        public SuperAdventure()
        {
            InitializeComponent();

            _player = PlayerDataMapper.CreateFromDatabase();

            if (_player == null)
            {
                _player = File.Exists(PLAYER_DATA_FILE_NAME)
                    ? Player.CreatePlayerFromXmlString(File.ReadAllText(PLAYER_DATA_FILE_NAME))
                    : Player.CreateDefaultPlayer();
            }

            _ = lblHitPoints.DataBindings.Add("Text", _player, "CurrentHitPoints");
            _ = lblGold.DataBindings.Add("Text", _player, "Gold");
            _ = lblExperience.DataBindings.Add("Text", _player, "ExperiencePoints");
            _ = lblLevel.DataBindings.Add("Text", _player, "Level");

            dgvInventory.RowHeadersVisible = false;
            dgvInventory.AutoGenerateColumns = false;
            dgvInventory.DataSource = _player.Inventory;
            _ = dgvInventory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 197,
                DataPropertyName = "Description"
            });
            _ = dgvInventory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Quantity",
                DataPropertyName = "Quantity"
            });

            dgvQuests.RowHeadersVisible = false;
            dgvQuests.AutoGenerateColumns = false;
            dgvQuests.DataSource = _player.Quests;

            _ = dgvQuests.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Name",
                Width = 197,
                DataPropertyName = "Name"
            });
            _ = dgvQuests.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Done?",
                DataPropertyName = "IsCompleted"
            });

            cboWeapons.DataSource = _player.Weapons;
            cboWeapons.DisplayMember = "Name";
            cboWeapons.ValueMember = "Id";

            if (_player.CurrentWeapon != null)
            {
                cboWeapons.SelectedItem = _player.CurrentWeapon;
            }

            cboWeapons.SelectedIndexChanged += cboWeapons_SelectedIndexChanged;

            cboPotions.DataSource = _player.Potions;
            cboPotions.DisplayMember = "Name";
            cboPotions.ValueMember = "Id";

            _player.PropertyChanged += PlayerOnPropertyChanged;
            _player.OnMessage += DisplayMessage;

            _player.MoveTo(_player.CurrentLocation);
        }

        private void DisplayMessage(object sender, MessageEventArgs messageEventArgs)
        {
            rtbMessages.Text += messageEventArgs.Message + Environment.NewLine;
            if (messageEventArgs.AddExtraNewLine)
            {
                rtbMessages.Text += Environment.NewLine;
            }

            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }
        private void PlayerOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Weapons")
            {
                cboWeapons.DataSource = _player.Weapons;
                if (!_player.Weapons.Any())
                {
                    cboWeapons.Visible = false;
                    btnUseWeapon.Visible = false;
                }
            }

            if (propertyChangedEventArgs.PropertyName == "Potions")
            {
                cboPotions.DataSource = _player.Potions;
                if (!_player.Potions.Any())
                {
                    cboPotions.Visible = false;
                    btnUsePotion.Visible = false;
                }
            }

            if (propertyChangedEventArgs.PropertyName == "CurrentLocation")
            {
                // Show/hide available movement buttons
                btnNorth.Visible = _player.CurrentLocation.LocationToNorth != null;
                btnEast.Visible = _player.CurrentLocation.LocationToEast != null;
                btnSouth.Visible = _player.CurrentLocation.LocationToSouth != null;
                btnWest.Visible = _player.CurrentLocation.LocationToWest != null;

                btnTrade.Visible = _player.CurrentLocation.VendorWorkingHere != null;

                // Display current location name and description
                rtbLocation.Text = _player.CurrentLocation.Name + Environment.NewLine;
                rtbLocation.Text += _player.CurrentLocation.Description + Environment.NewLine;

                btnUseWeapon.Visible =
                    cboWeapons.Visible = _player.CurrentLocation.HasAMonster && _player.Weapons.Any();

                btnUsePotion.Visible =
                    cboPotions.Visible = _player.CurrentLocation.HasAMonster && _player.Potions.Any();
            }
        }
        private void btnNorth_Click(object sender, EventArgs e) => _player.MoveNorth();
        private void btnEast_Click(object sender, EventArgs e) => _player.MoveEast();
        private void btnSouth_Click(object sender, EventArgs e) => _player.MoveSouth();
        private void btnWest_Click(object sender, EventArgs e) => _player.MoveWest();

        private void btnUseWeapon_Click(object sender, EventArgs e)
        {
            // Get the currently selected weapon from the cboWeapons ComboBox
            var currentWeapon = (Weapon)cboWeapons.SelectedItem;

            _player.UseWeapon(currentWeapon);
        }
        private void btnUsePotion_Click(object sender, EventArgs e)
        {
            // Get the currently selected potion from the combobox
            var potion = (HealingPotion)cboPotions.SelectedItem;
            _player.UsePotion(potion);
        }
        private void SuperAdventure_FormClosing(object sender, FormClosingEventArgs e)
        {
            File.WriteAllText(PLAYER_DATA_FILE_NAME, _player.ToXmlString());
            PlayerDataMapper.SaveToDatabase(_player);
        }
        private void cboWeapons_SelectedIndexChanged(object sender, EventArgs e) => _player.CurrentWeapon = (Weapon)cboWeapons.SelectedItem;
        private void btnTrade_Click(object sender, EventArgs e)
        {
            var tradingScreen = new TradingScreen(_player)
            {
                StartPosition = FormStartPosition.CenterParent
            };
            _ = tradingScreen.ShowDialog(this);
        }
    }
}
