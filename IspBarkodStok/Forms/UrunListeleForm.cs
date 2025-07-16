﻿using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace StokTakipOtomasyonu
{
    public partial class UrunListeleForm : Form
    {
        private MySqlConnection connection;
        private string connectionString = "server=localhost;database=stok_takip_otomasyonu;uid=root;pwd=;";
        private DataTable dataSourceTable;
        private Label lblUrunSayisi;

        public UrunListeleForm()
        {
            this.Icon = new Icon("isp_logo2.ico");
            InitializeComponent();
            connection = new MySqlConnection(connectionString);

            lblUrunSayisi = new Label();
            lblUrunSayisi.Location = new Point(20, dataGridView1.Bottom + 10);
            lblUrunSayisi.AutoSize = true;
            lblUrunSayisi.Font = new Font("Segoe UI", 9.5F);
            this.Controls.Add(lblUrunSayisi);

            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
            ProjeleriYukle();
            UrunleriYukle();
            this.Shown += (s, e) => txtArama.Focus();

        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Stok Miktarı" ||
                dataGridView1.Columns[e.ColumnIndex].Name == "Durum")
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
                if (row.Cells["Kritik Seviye"].Value != DBNull.Value &&
                    row.Cells["Stok Miktarı"].Value != DBNull.Value)
                {
                    int kritikSeviye = Convert.ToInt32(row.Cells["Kritik Seviye"].Value);
                    int stokMiktari = Convert.ToInt32(row.Cells["Stok Miktarı"].Value);

                    if (kritikSeviye > 0 && stokMiktari <= kritikSeviye)
                    {
                        e.CellStyle.BackColor = Color.LightPink;
                        e.CellStyle.ForeColor = Color.DarkRed;
                        if (dataGridView1.Columns[e.ColumnIndex].Name == "Stok Miktarı")
                        {
                            dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText =
                                $"Kritik stok seviyesi: {kritikSeviye}\nMevcut stok: {stokMiktari}";
                        }
                    }
                }
            }
        }

        private void EnsureConnectionClosed()
        {
            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        private void ProjeleriYukle()
        {
            try
            {
                EnsureConnectionClosed();
                connection.Open();

                string query = "SELECT proje_id, CONCAT(proje_kodu, ' - ', proje_tanimi) AS proje_bilgisi FROM projeler WHERE aktif = 1";
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbProjeler.DataSource = dt;
                cmbProjeler.DisplayMember = "proje_bilgisi";
                cmbProjeler.ValueMember = "proje_id";
                cmbProjeler.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Projeler yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        private void UrunleriYukle()
        {
            try
            {
                EnsureConnectionClosed();
                connection.Open();

                string query = @"SELECT 
                                u.urun_id AS 'ID',
                                u.urun_adi AS 'Ürün Adı',
                                u.urun_kodu AS 'Ürün Kodu',
                                u.urun_barkod AS 'Barkod',
                                u.urun_marka AS 'Marka',
                                u.miktar AS 'Stok Miktarı',
                                u.kritik_seviye AS 'Kritik Seviye',
                                IFNULL((SELECT SUM(pu.miktar)
                                        FROM proje_urunleri pu
                                        JOIN projeler p ON pu.proje_id = p.proje_id
                                        WHERE pu.urun_id = u.urun_id AND p.aktif = 1), 0) AS 'Projelerdeki Miktar',
                                CASE 
                                    WHEN u.kritik_seviye IS NOT NULL AND u.kritik_seviye > 0 AND u.miktar <= u.kritik_seviye 
                                    THEN 'KRİTİK SEVİYENİN ALTINDA' 
                                    ELSE '' 
                                END AS 'Durum'
                                FROM urunler u
                                ORDER BY u.urun_adi";

                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                dataSourceTable = new DataTable();
                adapter.Fill(dataSourceTable);

                dataGridView1.DataSource = dataSourceTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.Columns["ID"].Visible = false;
                dataGridView1.Columns["Kritik Seviye"].Visible = false;

                if (!dataGridView1.Columns.Contains("btnIslemGecmisi"))
                {
                    DataGridViewButtonColumn btnColumn = new DataGridViewButtonColumn();
                    btnColumn.Name = "btnIslemGecmisi";
                    btnColumn.HeaderText = "İşlemler";
                    btnColumn.Text = "İşlem Geçmişi";
                    btnColumn.UseColumnTextForButtonValue = true;
                    dataGridView1.Columns.Add(btnColumn);
                }

                if (lblUrunSayisi != null)
                {
                    lblUrunSayisi.Text = $"{dataSourceTable.Rows.Count} ürün listelendi.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        private void ProjeUrunleriniYukle(int projeId)
        {
            try
            {
                EnsureConnectionClosed();
                connection.Open();

                string query = @"SELECT 
                                u.urun_id AS 'ID',
                                u.urun_adi AS 'Ürün Adı',
                                u.urun_kodu AS 'Ürün Kodu',
                                u.urun_barkod AS 'Barkod',
                                u.urun_marka AS 'Marka',
                                pu.miktar AS 'Projedeki Miktar',
                                u.miktar AS 'Stok Miktarı',
                                u.kritik_seviye AS 'Kritik Seviye',
                                CASE 
                                    WHEN u.kritik_seviye IS NOT NULL AND u.kritik_seviye > 0 AND u.miktar <= u.kritik_seviye 
                                    THEN 'KRİTİK SEVİYENİN ALTINDA' 
                                    ELSE '' 
                                END AS 'Durum'
                                FROM proje_urunleri pu
                                JOIN urunler u ON pu.urun_id = u.urun_id
                                WHERE pu.proje_id = @projeId
                                ORDER BY u.urun_adi";

                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@projeId", projeId);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                dataSourceTable = new DataTable();
                adapter.Fill(dataSourceTable);

                dataGridView1.DataSource = dataSourceTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.Columns["ID"].Visible = false;
                dataGridView1.Columns["Kritik Seviye"].Visible = false;

                if (!dataGridView1.Columns.Contains("btnIslemGecmisi"))
                {
                    DataGridViewButtonColumn btnColumn = new DataGridViewButtonColumn();
                    btnColumn.Name = "btnIslemGecmisi";
                    btnColumn.HeaderText = "İşlemler";
                    btnColumn.Text = "İşlem Geçmişi";
                    btnColumn.UseColumnTextForButtonValue = true;
                    dataGridView1.Columns.Add(btnColumn);
                }

                if (lblUrunSayisi != null)
                {
                    lblUrunSayisi.Text = $"{dataSourceTable.Rows.Count} ürün listelendi.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        private void btnYenile_Click(object sender, EventArgs e)
        {
            cmbProjeler.SelectedIndex = -1;
            UrunleriYukle();
        }

        private void btnKapat_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtArama_TextChanged(object sender, EventArgs e)
        {
            string aramaKelimesi = txtArama.Text.Trim();
            if (dataSourceTable != null)
            {
                dataSourceTable.DefaultView.RowFilter =
                    $"`Ürün Adı` LIKE '%{aramaKelimesi}%' OR " +
                    $"`Ürün Kodu` LIKE '%{aramaKelimesi}%' OR " +
                    $"`Barkod` LIKE '%{aramaKelimesi}%' OR " +
                    $"`Marka` LIKE '%{aramaKelimesi}%'";
            }
        }

        private void cmbProjeler_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbProjeler.SelectedItem != null)
            {
                DataRowView selectedRow = cmbProjeler.SelectedItem as DataRowView;
                if (selectedRow != null)
                {
                    int projeId = Convert.ToInt32(selectedRow["proje_id"]);
                    ProjeUrunleriniYukle(projeId);
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["btnIslemGecmisi"].Index && e.RowIndex >= 0)
            {
                int urunId = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["ID"].Value);
                string urunAdi = dataGridView1.Rows[e.RowIndex].Cells["Ürün Adı"].Value.ToString();

                IslemGecmisiForm islemGecmisiForm = new IslemGecmisiForm(urunId, urunAdi);
                islemGecmisiForm.ShowDialog();
            }
        }

        private void UrunListeleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (connection != null)
            {
                EnsureConnectionClosed();
                connection.Dispose();
            }
        }
    }
}
