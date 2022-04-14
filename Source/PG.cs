using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Modes;
using Modes.BusinessLogic;
using System.Collections;
using Modes.NetAccess;

namespace ApiExternalTestApp
{
    public partial class MainWindow : Form
    {

        class IGO_Cover
        {
            public IGenObject IGO;
            public string DisplayName { get; set; }
        }

        class PlanType_Cover
        {
            public PlanType PlanType;
            public DateTime dtVersion;
            public DateTime dtTarget;
            public string DisplayName
            {
                get
                {
                    return string.Format("{0}, на {1}, получен {2}", ModesUtils.PlanTypeToString(PlanType), dtTarget.SystemToLocalHqEx().ToShortDateString(), dtVersion.SystemToLocalEx().ToString());
                }
            }
        }

        private void PGGetObjects_Click(object sender, EventArgs e)
        {

            /*
             * Date.LocalHqToSystemEx();
             *    внутренне представление начала суток планирования
             */
            DateTime dt_ = PGDate.Value.Date.LocalHqToSystemEx();
            SyncZone syncZone = (SyncZone)PGSync.SelectedIndex;

            IModesTimeSlice ts = null;
            if (api_ != null)
            {
                ts = api_.GetModesTimeSlice(dt_, syncZone
                    , TreeContent.PGObjects /*только оборудование, по которому СО публикует ПГ(включая родителей)*/
                    , false);

                PGObjects.Items.Clear();
                foreach (IGenObject IGO in ts.GenTree)
                    ExpandTree(PGObjects.Items, IGO, "");

                if (PGObjects.Items.Count > 0)
                    PGObjects.SelectedIndex = 0;
            }

            dataGridView1.Visible = false;
            outputBox.Visible = true;
        }


        private void PGGetTypes_Click(object sender, EventArgs e)
        {
            DateTime dt_ = PGDate.Value.Date.LocalHqToSystemEx();
            if (PGObjects.SelectedItem != null && PGObjects.SelectedItem is IGO_Cover)
            {

                PGType.Items.Clear();

                ///dt_ -- должна учказывать на начало суток паланирования в во внутреннем(системном времени), для чего и PGDate.Value.Date.LocalHqToSystemEx();
                foreach (var it in api_.GetPlanVersionsByDay(dt_, ((IGO_Cover)PGObjects.SelectedItem).IGO))
                {
                    PGType.Items.Add(new PlanType_Cover() { dtVersion = it.DTReceived, dtTarget = it.DT, PlanType = it.Type });
                }

                if (PGType.Items.Count > 0)
                    PGType.SelectedIndex = 0;

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PGGetData_Click(object sender, EventArgs e)
        {
            DateTime dt_ = PGDate.Value.Date.LocalHqToSystemEx();
            if (PGObjects.SelectedItem != null && PGObjects.SelectedItem is IGO_Cover &&
                PGType.SelectedItem != null && PGType.SelectedItem is PlanType_Cover)
            {

                var Values = api_.GetPlanByDay(dt_, new PlanType[] { (PGType.SelectedItem as PlanType_Cover).PlanType }, new IGenObject[] { ((IGO_Cover)PGObjects.SelectedItem).IGO });

                if (Values.Count == 0)
                {
                    MessageBox.Show(string.Format("На {0} нет плановых графиков", PGDate.Value.Date.ToShortDateString()));
                    return;
                }
                var Max = Values.Max(RRR => RRR.DT).SystemToLocalHqEx();
                var Min = Values.Min(RRR => RRR.DT).SystemToLocalHqEx();


                int interval = 60;

                if (((IGO_Cover)PGObjects.SelectedItem).IGO.TimeSlice.IdArea == 1)
                    interval = 30;

                DataGridPlanConstruction(Min, Max, interval);


                foreach (var it in api_.GetPlanFactors().OrderBy(RRR => RRR.Id))
                {
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[it.Id].Cells[0].Value = it.Description;
                }

                ///dt_ -- должна учказывать на начало суток паланирования в во внутреннем(системном времени), для чего и PGDate.Value.Date.LocalHqToSystemEx();
                foreach (var it in Values)
                {
                    int col = (int)((it.DT.SystemToLocalHqEx() - Min).TotalMinutes / interval) + 1;

                    dataGridView1.Rows[it.ObjFactor].Cells[col].Value = it.Value.ToString();

                }


                dataGridView1.Visible = true;

            }
        }


        private void PGActual_Click(object sender, EventArgs e)
        {
            DateTime dt_ = PGDate.Value.Date.LocalHqToSystemEx();
            if (PGObjects.SelectedItem != null && PGObjects.SelectedItem is IGO_Cover)
            {

                var Values = api_.GetPlanValuesActual(dt_, PGDate.Value.Date.AddDays(1).SystemToLocalHqEx(), ((IGO_Cover)PGObjects.SelectedItem).IGO);

                if (Values.Count == 0)
                {
                    MessageBox.Show(string.Format("На {0} нет плановых графиков", PGDate.Value.Date.ToShortDateString()));
                    return;
                }
                var Max = Values.Max(RRR => RRR.DT).SystemToLocalHqEx();
                var Min = Values.Min(RRR => RRR.DT).SystemToLocalHqEx();


                int interval = 60;

                if (((IGO_Cover)PGObjects.SelectedItem).IGO.TimeSlice.IdArea == 1)
                    interval = 30;

                DataGridPlanConstruction(Min, Max, interval);


                foreach (var it in api_.GetPlanFactors().OrderBy(RRR => RRR.Id))
                {
                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[it.Id].Cells[0].Value = it.Description;
                }

                ///dt_ -- должна учказывать на начало суток паланирования в во внутреннем(системном времени), для чего и PGDate.Value.Date.LocalHqToSystemEx();
                foreach (var it in Values)
                {
                    int col = (int)((it.DT.SystemToLocalHqEx() - Min).TotalMinutes / interval) + 1;

                    dataGridView1.Rows[it.ObjFactor].Cells[col].Value = it.Value.ToString();

                }


                dataGridView1.Visible = true;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dtStart">время СО</param>
        /// <param name="dtEnd">время СО</param>
        /// <param name="intreval"></param>
        void DataGridPlanConstruction(DateTime dtStart, DateTime dtEnd, double intreval)
        {
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();

            DataGridViewTextBoxColumn col1 = new DataGridViewTextBoxColumn();
            col1.HeaderText = "Характеристика";
            col1.Width = 140;

            dataGridView1.Columns.Add(col1);


            while (dtStart <= dtEnd)
            {
                col1 = new DataGridViewTextBoxColumn();
                col1.HeaderText = dtStart.ToString("dd HH:mm");
                col1.Width = 80;
                dataGridView1.Columns.Add(col1);
                dtStart = dtStart.AddMinutes(intreval);
            }

        }


        void ExpandTree(IList list, IGenObject IGO, string Level)
        {
            list.Add(new IGO_Cover()
            {
                IGO = IGO,
                DisplayName =
                String.Format("{0} {1}; Тип: {2}", Level, IGO.Description, IGO.GenObjType.Description)
            });
            foreach (var it in IGO.Children)
                ExpandTree(list, it, Level + "  ");
        }

        void api__OnPlanDataChanged(object sender, EventPlanDataChanged e)
        {
            newMessage = $"Получен плановыq график {ModesUtils.PlanTypeToString(e.Type)} по станции {e.ClientId} на {e.Day.SystemToLocalHqEx().ToShortDateString()}{Environment.NewLine}";
            this.Invoke(new MethodInvoker(AddEventMessage));
        }
    }
}
