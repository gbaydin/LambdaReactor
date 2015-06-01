using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

using LambdaReactor;

namespace LambdaUI
{
    public delegate void LogUpdateDelegate(string update, string[] pop);

    public partial class Form1 : Form
    {
        private Experiment R;

        private Graphics LabelDisplayG;
        private Bitmap LabelDisplayBuffer;

        public Form1()
        {
            InitializeComponent();

            LabelDisplayG = labelDisplay.CreateGraphics();
            LabelDisplayBuffer = new Bitmap(labelDisplay.Width, labelDisplay.Height);

            CheckForIllegalCrossThreadCalls = false;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            if (R != null)
                R.Stop();
            this.Close();
        }


        public void LogUpdate(string update, string[] pop)
        {
            switch (update)
            { 
                case "Reactor stopped":
                    buttonRun.Text = "Run";
                    break;
                default:
                    richTextBoxPop.Lines = pop;
                    break;
            }
            textBoxLog.AppendText(update + Environment.NewLine);
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (richTextBoxSeeds.TextLength > 0 )
            {
                if (buttonRun.Text == "Run")
                {
                    int PopulationSize = (int)numericUpDownReactorSize.Value;
                    int Epochs = (int)numericUpDownEpochs.Value;
                    int ReactionDelay = (int)numericUpDownReactionDelay.Value;
                    int ProductMaxLength = (int)numericUpDownProductMaxLength.Value;
                    float PAtomic = (float)numericUpDownPAtomic.Value;
                    float PAbstraction = (float)numericUpDownPAbstraction.Value;
                    int SeedMaxLength = (int)numericUpDownSeedMaxLength.Value;
                    bool CopyAllowed = !checkBoxCopyProhibit.Checked;
                    bool Perturbations = checkBoxPerturbation.Checked;
                    int PerturbationsCollisions = (int)numericUpDownPerturbationsCollisions.Value;
                    int PerturbationsObjects = (int)numericUpDownPerturbationsObjects.Value;
                    richTextBoxSeeds.Text = richTextBoxSeeds.Text.Trim();
                    string[] Seeds = richTextBoxSeeds.Lines;

                    buttonRun.Text = "Stop";
                    R = new Experiment(PopulationSize, Epochs, ReactionDelay, ProductMaxLength, PAtomic, PAbstraction, SeedMaxLength, CopyAllowed, Perturbations, PerturbationsCollisions, PerturbationsObjects, Seeds, new LogUpdateDelegate(LogUpdate), LabelDisplayG, LabelDisplayBuffer, labelDisplay.BackColor);
                    R.Run();
                }
                else
                {
                    if (R != null)
                    {
                        R.Stop();
                        buttonRun.Text = "Run";
                    }

                }
            }
        }

        private void buttonRandom_Click(object sender, EventArgs e)
        {
            int NumSeeds = (int)numericUpDownNumSeeds.Value;
            int SeedMaxLength = (int)numericUpDownSeedMaxLength.Value;
            float PAtomic = (float)numericUpDownPAtomic.Value;
            float PAbstraction = (float)numericUpDownPAbstraction.Value;

            string[] seeds = new string[NumSeeds];

            for (int i = 0; i < NumSeeds; i++ )
            {
                seeds[i] = LambdaReactor.Reactor.randomExp(PAtomic, PAbstraction, SeedMaxLength);
            }
            richTextBoxSeeds.Lines = seeds;
            richTextBoxPop.Clear();
        }

        private void labelDisplay_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(LabelDisplayBuffer, 0, 0, LabelDisplayBuffer.Width, LabelDisplayBuffer.Height);
        }

        private void buttonCopy_Click(object sender, EventArgs e)
        {
            richTextBoxSeeds.Text = richTextBoxPop.Text;
            richTextBoxPop.Clear();
        }

        private void checkBoxSelfCopy_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCopyProhibit.Checked)
                checkBoxCopyProhibit.Text = "Not Allowed";
            else
                checkBoxCopyProhibit.Text = "Allowed";
        }

        private void checkBoxPerturbation_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPerturbation.Checked)
                checkBoxPerturbation.Text = "Enabled";
            else
                checkBoxPerturbation.Text = "Disabled";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (R != null)
                R.Stop();
        }

        private void numericUpDownPAtomic_ValueChanged(object sender, EventArgs e)
        {
            numericUpDownPAbstraction.Maximum = 1 - numericUpDownPAtomic.Value;
            numericUpDownPApplication.Value = 1 - numericUpDownPAtomic.Value - numericUpDownPAbstraction.Value;
        }

        private void numericUpDownPAbstraction_ValueChanged(object sender, EventArgs e)
        {
            numericUpDownPAtomic.Maximum = 1 - numericUpDownPAbstraction.Value;
            numericUpDownPApplication.Value = 1 - numericUpDownPAtomic.Value - numericUpDownPAbstraction.Value;
        }
    }

    public class Graph2D
    {
        private Brush BackColor;
        private Brush ForeColor;
        private Font f;
        private int Components;
        private ArrayList[] Data;
        private Pen[] ComponentColors;
        private string[] ComponentNames;
        private Graphics G;
        private RectangleF Position;
        private RectangleF GraphPosition;
        private PointF[][] Points;
        private bool NeedsUpdate;

        public Graph2D(int components, Graphics g, RectangleF position, Color backColor, Color foreColor)
        {
            BackColor = new SolidBrush(backColor);
            ForeColor = new SolidBrush(foreColor);
            f = new Font("Segoe UI", 8);
            Components = components;
            Data = new ArrayList[Components];
            ComponentColors = new Pen[Components];
            ComponentNames = new string[Components];
            for (int i = 0; i < Components; i++)
            {
                Data[i] = new ArrayList(100);
                ComponentColors[i] = new Pen(foreColor);
                ComponentNames[i] = "";
            }
            G = g;
            Position = position;
            GraphPosition = new RectangleF(Position.X, Position.Y, Position.Width, Position.Height - Components * 10);
            NeedsUpdate = false;
        }

        public void SetComponent(int component, string name, Color color)
        {
            if (component < Components && component > -1)
            {
                ComponentColors[component] = new Pen(color, 2);
                ComponentNames[component] = name;
            }
        }

        public void AddValue(int component, float val)
        {
            Data[component].Add(val);
            NeedsUpdate = true;
        }

        public void ChangeLastValue(int component, float val)
        {
            Data[component][Data[component].Count - 1] = val;
            NeedsUpdate = true;
        }

        public void UpdatePoints()
        {
            float datamax = float.MinValue;
            float datamin = float.MaxValue;
            float dat;
            for (int i = 0; i < Components; i++)
            {
                for (int d = 0; d < Data[i].Count; d++)
                {
                    dat = (float)Data[i][d];
                    if (dat >= datamax)
                        datamax = dat;
                    if (dat <= datamin)
                        datamin = dat;
                }
            }
            float hstep = GraphPosition.Width / (Data[0].Count - 1);
            float range = datamax - datamin;
            if (range == 0)
                range = 1;
            float vstep = GraphPosition.Height / range;

            Points = new PointF[Components][];
            for (int i = 0; i < Components; i++)
            {
                Points[i] = new PointF[Data[i].Count];
                for (int p = 0; p < Points[i].Length; p++)
                    Points[i][p] = new PointF(GraphPosition.X + p * hstep, GraphPosition.Bottom - ((float)Data[i][p] - datamin) * vstep);
            }

            NeedsUpdate = false;
        }

        public void Draw()
        {
            if (Data[0].Count > 1)
            {
                if (NeedsUpdate)
                    UpdatePoints();
                G.FillRectangle(BackColor, Position);
                for (int i = 0; i < Data.Length; i++)
                {
                    G.DrawLines(ComponentColors[i], Points[i]);
                    float y = GraphPosition.Bottom + i * 10;
                    G.DrawString(ComponentNames[i] + " " + ((float)Data[i][Data[i].Count - 1]).ToString("f2"), f, ForeColor, GraphPosition.X + 20, y);
                    G.DrawLine(ComponentColors[i], GraphPosition.X, y + 5, GraphPosition.X + 15, y + 5);
                }
            }
        }
    }

    public class GraphHistogram
    {
        private Graphics G;
        private RectangleF Position;
        private RectangleF GraphPosition;
        private Brush BackColor;
        private Brush ForeColor;
        private Brush GraphColor;
        private int Steps;
        private float Min;
        private float Max;
        private float StepRange;
        private Font f;
        private bool NeedsUpdate;
        private ArrayList Data;
        private RectangleF[] Rectangles;
        private float HStep;
        private string Name;

        public GraphHistogram(string name, int steps, Graphics g, RectangleF position, Color backColor, Color foreColor, Color graphColor)
        {
            Name = name;
            G = g;
            Position = position;
            GraphPosition = new RectangleF(Position.X, Position.Y, Position.Width, Position.Height - 20);
            f = new Font("Segoe UI", 8);
            BackColor = new SolidBrush(backColor);
            ForeColor = new SolidBrush(foreColor);
            GraphColor = new SolidBrush(graphColor);
            Steps = steps;
            HStep = GraphPosition.Width / Steps;
            Data = new ArrayList(100);
            NeedsUpdate = true;
        }

        public void Clear()
        {
            Data.Clear();
            NeedsUpdate = true;
        }

        public void AddValue(float val)
        {
            Data.Add(val);
            NeedsUpdate = true;
        }

        public void SetValues(float[] val)
        {
            Data.Clear();
            Data.AddRange(val);
            NeedsUpdate = true;
        }

        private void UpdateSteps()
        {
            Min = float.MaxValue;
            Max = float.MinValue;
            float v;
            for (int i = 0; i < Data.Count; i++)
            {
                v = (float)Data[i];
                if (v >= Max)
                    Max = v;
                if (v <= Min)
                    Min = v;
            }
            StepRange = (Max - Min) / Steps;

            int[] frequencies = new int[Steps];
            float dv;
            int frequenciesmax = 0;
            for (int i = 0; i < Steps; i++)
            {
                frequencies[i] = 0;
                for (int d = 0; d < Data.Count; d++)
                {
                    dv = (float)Data[d];
                    if ((dv >= Min + i * StepRange) && (dv <= Min + (i + 1) * StepRange))
                        frequencies[i]++;
                }
                if (frequencies[i] >= frequenciesmax)
                    frequenciesmax = frequencies[i];
            }
            float vstep = GraphPosition.Height / frequenciesmax;
            Rectangles = new RectangleF[Steps];
            float h;
            for (int i = 0; i < Steps; i++)
            {
                h = frequencies[i] * vstep;
                Rectangles[i] = new RectangleF(GraphPosition.X + i * HStep + 2, GraphPosition.Bottom - h, HStep - 4, h);
            }
        }

        public void Draw()
        {
            if (NeedsUpdate)
                UpdateSteps();
            if (!float.IsNaN(Rectangles[0].Height))
            {
                G.FillRectangle(BackColor, Position);
                G.FillRectangles(GraphColor, Rectangles);
                G.DrawString(Min.ToString("f2"), f, ForeColor, GraphPosition.X, GraphPosition.Bottom);
                G.DrawString(Max.ToString("f2"), f, ForeColor, GraphPosition.Right - G.MeasureString(Max.ToString("f2"), f).Width, GraphPosition.Bottom);
                G.DrawString(Name, f, ForeColor, GraphPosition.X, GraphPosition.Bottom + 10);
            }
        }
    }

    public class Experiment
    {
        private int PopulationSize;
        private int Reactions;
        private int ReactionDelay;
        private int ProductMaxLength;
        private int SeedMaxLength;
        private bool CopyAllowed;
        private bool Perturbations;
        private int PerturbationsCollisions;
        private int PerturbationsObjects;
        private float PAtomic;
        private float PAbstraction;
        private string[] Seeds;
        private string[] Pop;
        private Thread T;
        private LogUpdateDelegate LogUpdate;

        private Graphics DisplayG;
        private Bitmap DisplayBuffer;
        private Graphics DisplayBufferG;
        private Color BackColor;
        private Font F10;
        private Font F12;
        private Font F16;
        private Font F32;
        private Color Background;
        private Graph2D GAverageLength;
        private Graph2D GDiversity;
        private GraphHistogram GLengthHistogram;

        private double AverageLength;
        private double MinLength;
        private double MaxLength;
        private double Diversity;

        public Experiment(int populationSize, int reactions, int reactionDelay, int productMaxLength, float pAtomic, float pAbstraction, int seedMaxLength, bool copyAllowed, bool perturbations, int perturbationsCollisions, int perturbationsObjects, string[] seeds, LogUpdateDelegate logUpdate, Graphics displayG, Bitmap displayBuffer, Color backColor)
        {
            PopulationSize = populationSize;
            Reactions = reactions;
            ReactionDelay = reactionDelay;
            ProductMaxLength = productMaxLength;
            PAtomic = pAtomic;
            PAbstraction = pAbstraction;
            SeedMaxLength = seedMaxLength;
            CopyAllowed = copyAllowed;
            Perturbations = perturbations;
            PerturbationsCollisions = perturbationsCollisions;
            PerturbationsObjects = perturbationsObjects;
            Seeds = seeds;
            LogUpdate = logUpdate;

            DisplayG = displayG;
            DisplayBuffer = displayBuffer;
            DisplayBufferG = Graphics.FromImage(DisplayBuffer);
            DisplayBufferG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            BackColor = backColor;
            F10 = new Font("Segoe UI", 10);
            F12 = new Font("Segoe UI", 12);
            F16 = new Font("Segoe UI", 16);
            F32 = new Font("Segoe UI", 32);
            Background = Color.FromArgb(22, 23, 22);
            GAverageLength = new Graph2D(3, DisplayBufferG, new RectangleF(240, 10, 220, 150), BackColor, Color.White);
            GAverageLength.SetComponent(0, "Max length", Color.White);
            GAverageLength.SetComponent(1, "Mean length", Color.Silver);
            GAverageLength.SetComponent(2, "Min length", Color.Gray);
            GDiversity = new Graph2D(1, DisplayBufferG, new RectangleF(470, 10, 220, 150), BackColor, Color.White);
            GDiversity.SetComponent(0, "Reactor diversity", Color.White);
            GLengthHistogram = new GraphHistogram("Length distribution", 15, DisplayBufferG, new RectangleF(700, 10, 220, 150), BackColor, Color.White, Color.LightGray);

            LogUpdate("Reactor initialized", null);
        }

        public void Run()
        {
            T = new Thread(new ThreadStart(RunThread));
            LogUpdate("Reactor started", null);
            T.Start();
        }

        public void Stop()
        {
            if (T != null)
            {
                T.Abort();
                LogUpdate("Reactor stopped", null);
            }
        }

        public void RunThread()
        {
            LogUpdate("Initializing reactor", null);

            Pop = Reactor.initPop(PopulationSize, Seeds);
            HashSet<string> uniques = new HashSet<string>();
            
            for (int r = 1; r <= Reactions; r++)
            {
                string result = Reactor.collide(out Pop, ProductMaxLength, CopyAllowed);
                LogUpdate(result, Pop);


                if (r % PerturbationsCollisions == 0)
                    if (Perturbations)
                    {
                        Reactor.perturb(out Pop, PerturbationsObjects, PAtomic, PAbstraction, SeedMaxLength);
                        LogUpdate("Perturbation", Pop);
                    }

                AverageLength = 0;
                MaxLength = 0;
                MinLength = double.MaxValue;
                uniques.Clear();
                List<float> HistogramLength = new List<float>();
                for (int i = 0; i < Pop.Length; i++)
                {
                    AverageLength += Pop[i].Length;
                    if (Pop[i].Length >= MaxLength)
                        MaxLength = Pop[i].Length;
                    if (Pop[i].Length <= MinLength)
                        MinLength = Pop[i].Length;
                    HistogramLength.Add((float)Pop[i].Length);
                    uniques.Add(Pop[i]);
                }
                Diversity = (double)uniques.Count / (double)PopulationSize;
                AverageLength /= (double)Pop.Length;

                GAverageLength.AddValue(0, (float)MaxLength);
                GAverageLength.AddValue(1, (float) AverageLength);
                GAverageLength.AddValue(2, (float)MinLength);
                GDiversity.AddValue(0, (float)Diversity);

                GLengthHistogram.SetValues(HistogramLength.ToArray());

                DrawStatus(r, (double)r / (double)Reactions);
                
                Thread.Sleep(ReactionDelay);
            }

        }

        private void DrawStatus(int reaction, double percentage)
        {
            DisplayBufferG.Clear(Background);
            DisplayBufferG.DrawString("Collision", F12, Brushes.White, 0, 0);
            DisplayBufferG.DrawString(reaction.ToString(), F32, Brushes.White, 0, 15);
            DisplayBufferG.DrawString((percentage * 100).ToString() + "%", F10, Brushes.White, 4, 130);
            DisplayBufferG.FillRectangle(Brushes.DimGray, 4, 150, 210, 8);
            DisplayBufferG.FillRectangle(Brushes.Gainsboro, 4, 150, (float)(210.0 * percentage), 8);
            if (reaction > 0)
            {
                GAverageLength.Draw();
                GDiversity.Draw();
                GLengthHistogram.Draw();
            }
            DisplayG.DrawImage(DisplayBuffer, 0, 0, DisplayBuffer.Width, DisplayBuffer.Height);
        }



    }
}
