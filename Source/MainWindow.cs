using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Modes;
using Modes.DataAccess;
using Modes.BusinessLogic;
using System.Collections.ObjectModel;
using Modes.NetAccess;
using ModesApiExternal;

namespace ApiExternalTestApp
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool isConnected_;
        public bool isConnected
        {
            get
            {
                return isConnected_;
            }
            set
            {
                if (isConnected_ != value)
                    isConnected_ = value;

                EnableWindowElements(isConnected);
            }
        }

        private IApiExternal api_ = null;

        void EnableWindowElements(bool IsConnected)
        {
            if (IsConnected)
            {
                GlobalPanel.Enabled = true;
            }
            else
            {
                GlobalPanel.Enabled = false;
            }
        }

        private void подключитьсяToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                подключитьсяToolStripMenuItem.Text = "Подключиться";
                ModesApiFactory.CloseConnection();
                isConnected = false;
            }
            else
            {
                ConnectionForm_ f1 = new ConnectionForm_();
                if (f1.ShowDialog() != DialogResult.OK)
                    return;

                if (f1.isLocal)
                    ModesApiFactory.Initialize(f1.serverName, f1.User, f1.Pass);
                else
                    ModesApiFactory.Initialize(f1.serverName);


                if (ModesApiFactory.IsInitilized)
                {
                    api_ = ModesApiFactory.GetModesApi();

                    api_.OnData53500Modified += new EventHandler<EventRefreshData53500>(api__OnData53500Modified);
                    api_.OnMaket53500Changed += new EventHandler<EventRefreshJournalMaket53500>(api__OnMaket53500Changed);
                    api_.OnNsiModified += new EventHandler<EventUpdateNsi>(api__OnNSIModified);
                    api_.OnClose += new EventHandler(api__ServiceOnClose);
                    api_.OnPlanDataChanged += new EventHandler<EventPlanDataChanged>(api__OnPlanDataChanged);
                    isConnected = true;
                    подключитьсяToolStripMenuItem.Text = "Отключиться";
                }
            }
        }

        #region События
        string newMessage = "";
        private SyncZone syncZone;

        void api__OnData53500Modified(object self, EventRefreshData53500 e)
        {
            newMessage = String.Format("Изменение данных 53500. Кол-во оборудования: {0} \r\n", e.Equipments.Count);
            this.Invoke(new MethodInvoker(AddEventMessage));

            if ((listGenObjectForGetVarParam != null) && (e.Equipments.ContainsKey(dateTimePicker4.Value.Date.LocalHqToSystemEx())))
            {
                api_.RefreshGenObjects(listGenObjectForGetVarParam, dateTimePicker4.Value.Date.LocalHqToSystemEx(), syncZone);
                this.Invoke(new MethodInvoker(RefreshGridByObject));
            }
        }

        void api__OnMaket53500Changed(object self, EventRefreshJournalMaket53500 e)
        {
            newMessage = String.Format("Изменение макетов 53500. Кол-во макетов: {0} \r\n", e.countMakets.ToString());
            this.Invoke(new MethodInvoker(AddEventMessage));
        }

        void api__OnNSIModified(object self, EventUpdateNsi e)
        {
            if (e.ModelType == ModelType.M53500)
                newMessage = String.Format("Изменение НСИ {0} на дату: {1} \r\n", "53500", e.TargetDate.ToString());
            else
                if (e.ModelType == ModelType.M53101)
                newMessage = String.Format("Изменение НСИ {0} на дату: {1} \r\n", "53101", e.TargetDate.ToString());

            this.Invoke(new MethodInvoker(AddEventMessage));
        }

        void api__ServiceOnClose(object self, EventArgs e)
        {
            this.Invoke(new MethodInvoker(CloseMessage));
        }

        void CloseMessage()
        {
            MessageBox.Show("Сервис MODES был остановлен. Дальнейшая работа с приложением невозможна.", "Остановка работы сервиса MODES", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }


        void AddEventMessage()
        {
            outputEventsBox.Text = outputEventsBox.Text + newMessage;
        }
        #endregion

        #region Получение среза данных
        private void button1_Click(object sender, EventArgs e)
        {
            DateTime dt_ = dateTimePicker1.Value.Date.LocalHqToSystemEx();
            SyncZone syncZone = (SyncZone)comboBox1.SelectedIndex;
            TreeContent content = (TreeContent)comboBox3.SelectedIndex;
            bool loadData_ = checkBox1.Checked;

            IModesTimeSlice ts = null;
            if (api_ != null)
            {
                ts = api_.GetModesTimeSlice(dt_, syncZone, content, loadData_);

                IList<IGenObject> list = null;

                list = ts.GetGenObjectFlatList();

                outputBox.Items.Clear();
                foreach (IGenObject obj in list)
                {
                    outputBox.Items.Add(String.Format("Имя: {0}; Наименование: {1}; Тип: {2} \r\n", obj.Name, obj.Description, obj.GenObjType.Name));
                }
            }

            dataGridView1.Visible = false;
            outputBox.Visible = true;
        }
        #endregion

        #region Работа со слоями данных
        private void button2_Click(object sender, EventArgs e)
        {
            DateTime verifydate_ = dateTimePicker2.Value.LocalHqToSystemEx();
            DateTime dayPlanning_ = dateTimePicker3.Value.Date.LocalHqToSystemEx();
            SyncZone syncZone = (SyncZone)comboBox4.SelectedIndex;

            ModesLayer layer_;
            if (api_ != null)
            {
                layer_ = api_.GetCurrentLayer(verifydate_, dayPlanning_, syncZone);

                outputBox.Items.Clear();

                if (layer_ != null)
                    outputBox.Items.Add(String.Format("Имя слоя данных: {0} \r\n", layer_.Name));
            }

            dataGridView1.Visible = false;
            outputBox.Visible = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SyncZone syncZone = (SyncZone)comboBox5.SelectedIndex;

            IList<ModesLayer> listLayers_ = null;
            if (api_ != null)
            {
                listLayers_ = api_.GetLayersBySyncZone(syncZone);

                outputBox.Items.Clear();
                foreach (ModesLayer lay in listLayers_)
                {
                    outputBox.Items.Add(String.Format("Имя слоя данных: {0} \r\n", lay.Name));
                }
            }

            dataGridView1.Visible = false;
            outputBox.Visible = true;
        }
        #endregion

        #region Получение значений переменных характеристик
        int colCount = 0;

        IList<IGenObject> listGenObjectForGetVarParam = null;
        IList<ModesLayer> listLayersForGetVarParam = null;
        private void button4_Click(object sender, EventArgs e)
        {
            DateTime dt_ = dateTimePicker4.Value.Date.LocalHqToSystemEx();

            if (comboBox8.SelectedItem == null)
                return;

            syncZone = (SyncZone)comboBox8.SelectedIndex;
            ModelType model_ = ModelType.M53500;

            IModesTimeSlice tsForGetVarParam = null;

            if (api_ != null)
            {
                tsForGetVarParam = api_.GetModesTimeSlice(dt_, syncZone, TreeContent.AllObjects, true);
                colCount = 86400 / tsForGetVarParam.GridStep;
                if (model_ == ModelType.M53500)
                    listGenObjectForGetVarParam = tsForGetVarParam.GetGenObjectFlatList();
                else
                    listGenObjectForGetVarParam = tsForGetVarParam.GenTree;

                outputBox.Items.Clear();
                if (listGenObjectForGetVarParam != null)
                    foreach (IGenObject obj in listGenObjectForGetVarParam)
                    {
                        outputBox.Items.Add(String.Format("Имя: {0}; Наименование: {1}; Тип: {2} \r\n", obj.Name, obj.Description, obj.GenObjType.Name));
                    }


                comboBox9.Items.Clear();
                if (listGenObjectForGetVarParam != null)
                    foreach (IGenObject obj in listGenObjectForGetVarParam)
                    {
                        bool isExist = false;
                        foreach (string str in comboBox9.Items)
                            if (obj.GenObjType.Name == str)
                            {
                                isExist = true; break;
                            }

                        if (!isExist)
                            comboBox9.Items.Add(obj.GenObjType.Name);

                    }

                if (comboBox9.Items.Count > 0)
                    comboBox9.SelectedIndex = 0;

                listLayersForGetVarParam = api_.GetLayersBySyncZone(syncZone);

                comboBox6.Items.Clear();
                foreach (ModesLayer layer_ in listLayersForGetVarParam)
                    comboBox6.Items.Add(layer_.Name);

                if (comboBox6.Items.Count > 0)
                    comboBox6.SelectedIndex = 0;


                groupBox5.Enabled = true;
                button5.Enabled = true;

                dataGridView1.Visible = false;
                outputBox.Visible = true;
            }
        }

        private void RefreshGridByObject()
        {


            string genObjectTypeName = "";
            string genObjectName = "";

            if (checkBox2.Checked)
                genObjectTypeName = comboBox9.Text;
            else
                genObjectTypeName = textBox1.Text;

            if (checkBox3.Checked)
                genObjectName = comboBox10.Text;
            else
                genObjectName = textBox2.Text;

            var dt_ = dateTimePicker4.Value.Date.LocalHqToSystemEx();
            ModesLayer lay = null;

            if (comboBox6.Text != "Последние записаные данные")
            {
                foreach (var T in listLayersForGetVarParam)
                    if (T.Name == comboBox6.Text)
                    {
                        lay = T;
                        break;
                    }
            }
            else
            {
                /*получим актуальный слой*/

                lay = api_.GetCurrentLayer(DateTime.UtcNow, dt_, syncZone);
                if (lay == null)
                {
                    /*планирование завершено*/
                    lay = listLayersForGetVarParam[listLayersForGetVarParam.Count - 1];
                }

            }

            if (lay != null)
                if (listGenObjectForGetVarParam != null)
                    foreach (IGenObject obj in listGenObjectForGetVarParam)
                        if (obj.GenObjType.Name == genObjectTypeName && obj.Name == genObjectName)
                        {
                            var L = new List<IGenObject>();
                            L.Add(obj);
                            /*обновим данные*/
                            api_.RefreshGenObjects(L, dt_, radioButton3.Checked ? 1 : 0, lay.OffsetLocal, (ModesTaskType)lay.IdTask, syncZone);
                            dataGridView1.Visible = true;
                            outputBox.Visible = false;
                            DataGridConstruction(colCount, obj.VarParams);
                            break;
                        }
        }

        void DataGridConstruction(int columnCount, ReadOnlyCollection<IVarParam> varParams)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
            col1.HeaderText = "Характеристика";
            dataGridView1.Columns.Add(col1);

            for (int i = 1; i < columnCount + 1; i++)
            {
                DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
                col.HeaderText = i.ToString();
                col.Width = 40;
                dataGridView1.Columns.Add(col);
            }

            foreach (IVarParam param in varParams)
            {
                dataGridView1.Rows.Add();

                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Value = param.Name;
                for (int i = 1; i < param.PointCount + 1; i++)
                {
                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[i].Value = param.GetValue(i - 1);
                }
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            RefreshGridByObject();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                comboBox9.Visible = true;
                textBox1.Visible = false;
            }
            else
            {
                comboBox9.Visible = false;
                textBox1.Visible = true;
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                comboBox10.Visible = true;
                textBox2.Visible = false;
            }
            else
            {
                comboBox10.Visible = false;
                textBox2.Visible = true;
            }
        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox10.Items.Clear();
            if (listGenObjectForGetVarParam != null)
                foreach (IGenObject obj in listGenObjectForGetVarParam)
                    if (obj.GenObjType.Name == comboBox9.Text)
                        comboBox10.Items.Add(obj.Name);

            if (comboBox10.Items.Count > 0)
                comboBox10.SelectedIndex = 0;
        }
        #endregion

        #region Работа с макетами
        void DataGridMaketConstruction()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
            col1.HeaderText = "Отправитель";
            col1.Width = 140;

            dataGridView1.Columns.Add(col1);

            DataGridViewTextBoxColumn col6 = new DataGridViewTextBoxColumn();
            col6.HeaderText = "Дата отправки";
            col6.Width = 130;
            dataGridView1.Columns.Add(col6);

            DataGridViewTextBoxColumn col7 = new DataGridViewTextBoxColumn();
            col7.HeaderText = "Макет на дату";
            col7.Width = 130;
            dataGridView1.Columns.Add(col7);

            DataGridViewTextBoxColumn col8 = new DataGridViewTextBoxColumn();
            col8.HeaderText = "Этап планирования";
            col8.Width = 170;
            dataGridView1.Columns.Add(col8);

            DataGridViewTextBoxColumn col9 = new DataGridViewTextBoxColumn();
            col9.HeaderText = "Uid";
            col9.Width = 170;
            col9.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns.Add(col9);

        }
        private void refreshComboBoxUid(DateTime dt1_, DateTime dt2_)
        {
            comboBox11.Items.Clear();

            IList<IMaketHeader> listMaketHeader = api_.GetMaketHeaders53500(dt1_, dt2_);

            if (listMaketHeader == null)
                return;

            foreach (IMaketHeader row in listMaketHeader)
            {
                comboBox11.Items.Add(row.Mrid);
            }

            if (comboBox11.Items.Count > 0)
                comboBox11.SelectedIndex = 0;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            dataGridView1.Visible = true;
            DataGridMaketConstruction();

            DateTime dt1_ = dateTimePicker5.Value.Date.LocalHqToSystemEx();
            DateTime dt2_ = dateTimePicker6.Value.Date.LocalHqToSystemEx();

            IList<IMaketHeader> listMaketHeader = api_.GetMaketHeaders53500(dt1_, dt2_);

            if (listMaketHeader == null)
                return;

            foreach (IMaketHeader row in listMaketHeader)
            {
                dataGridView1.Rows.Add();

                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Value = row.Sender;
                if (row.DtSent != null)
                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Value = row.DtSent.Value.SystemToLocalHqEx().ToString();
                else
                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[1].Value = "...";
                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[2].Value = row.DtTarget.SystemToLocalHqEx().ToString();
                switch (row.Id_Task)
                {
                    case 0: dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[3].Value = "ВСВГО"; break;
                    case 1: dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[3].Value = "РСВ"; break;
                    case 2: dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[3].Value = "ОУ"; break;
                }

                dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[4].Value = row.Mrid.ToString();
            }

            refreshComboBoxUid(dt1_, dt2_);

            groupBox7.Enabled = true;
            button7.Enabled = true;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            dataGridView1.Visible = false;
            outputBox.Visible = true;
            outputBox.Items.Clear();

            if (comboBox11.SelectedItem == null)
                return;

            Guid uid = (Guid)comboBox11.SelectedItem;

            IList<IMaketEquipment> listMaketEquipment = api_.GetMaket53500Equipment((new Guid[] { uid }).ToList());

            if (listMaketEquipment == null)
                return;

            foreach (IMaketEquipment item in listMaketEquipment)
            {
                outputBox.Items.Add(String.Format("Макет :   {0}", item.Mrid.ToString()));
                outputBox.Items.Add("");

                outputBox.Items.Add("Оборудование:");
                List<IGenObject> objFlatList = new List<IGenObject>();

                foreach (IGenObject obj in item.GenTree)
                    objFlatList.Add(obj);

                for (int i = 0; i < objFlatList.Count; i++)
                    if (objFlatList[i].Children.Count > 0)
                        foreach (IGenObject obj in objFlatList[i].Children)
                            objFlatList.Add(obj);

                foreach (IGenObject obj in objFlatList)
                    outputBox.Items.Add("     " + obj.GenObjType.Name + "  " + obj.Name);

            }
        }
        #endregion


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == sdcTab)
                initSDCTab();
        }


        #region SDC
        void initSDCTab()
        {
            if (api_ != null && sdcStCB.Items.Count == 0)
            {
                var slice = api_.GetModesTimeSlice(DateTime.Now.Date.LocalHqToSystemEx(), SyncZone.First, TreeContent.AllObjects, false);
                sdcStCB.Items.AddRange(slice.GetGenObjectFlatList().Where(rrr => rrr.GenObjType.IsStation).OrderBy(rrr => rrr.Description).ToArray());

                if (sdcStCB.Items.Count > 0)
                    sdcStCB.SelectedIndex = 0;
                sdcFromDtP.Value = sdcToDtP.Value = DateTime.Now.Date;
            }
        }
        private void sdcQB_Click(object sender, EventArgs e)
        {
            var obj = sdcStCB.SelectedItem as IGenObject;
            if (obj != null)
            {
                dataGridView1.Visible = true;
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
                {
                    HeaderText = "Пакет СДК",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                });

                var rez = api_.GetSdcPackRaw(obj.Name, sdcFromDtP.Value.LocalHqToSystemEx(), sdcToDtP.Value.LocalHqToSystemEx());
                if (!rez.Completed)
                {
                    MessageBox.Show(this, rez.Message);
                }
                else
                {
                    foreach (var row in rez.Data)
                        dataGridView1.Rows[dataGridView1.Rows.Add()].Cells[0].Value = row.Xml;
                }
            }
        }
        #endregion

    }
}
