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
    public delegate void LogUpdateDelegate(string update, object detail);

    public partial class Form1 : Form
    {
        private Experiment R;
        private string[] Pop;

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

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void LogUpdate(string update, object detail)
        {
            switch (update)
            { 
                case "Reactor stopped":
                    buttonRun.Text = "Run";
                    break;
                case "New collision":
                    Pop = (string[]) detail;
                    richTextBoxPop.Lines = Pop;
                    break;
                default:
                    break;
            }
            textBoxLog.Text += update + Environment.NewLine;
            textBoxLog.SelectionStart = textBoxLog.Text.Length - 1;
            textBoxLog.ScrollToCaret();

        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (richTextBoxSeeds.TextLength > 0 )
            {
                if (buttonRun.Text == "Run")
                {
                    int PopulationSize = int.Parse(textBoxPopulationSize.Text);
                    int Reactions = int.Parse(textBoxReactions.Text);
                    int ReactionDelay = int.Parse(textBoxReactionDelay.Text);
                    int ProductMaxLength = int.Parse(textBoxProductMaxLength.Text);
                    int SeedMaxLength = int.Parse(textBoxSeedMaxLength.Text);
                    bool CopyAllowed = !checkBoxCopyProhibit.Checked;
                    bool Perturbations = checkBoxPerturbation.Checked;
                    int PerturbationsCollisions = int.Parse(textBoxPerturbationsCollisions.Text);
                    int PerturbationsObjects = int.Parse(textBoxPerturbationsObjects.Text);
                    richTextBoxSeeds.Text = richTextBoxSeeds.Text.Trim();
                    string[] Seeds = richTextBoxSeeds.Lines;

                    buttonRun.Text = "Stop";
                    R = new Experiment(PopulationSize, Reactions, ReactionDelay, ProductMaxLength, SeedMaxLength, CopyAllowed, Perturbations, PerturbationsCollisions, PerturbationsObjects, Seeds, new LogUpdateDelegate(LogUpdate), LabelDisplayG, LabelDisplayBuffer, labelDisplay.BackColor);
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

        private void buttonTest_Click(object sender, EventArgs e)
        {
            //richTextBoxSeeds.ResetText();
            //richTextBoxSeeds.AppendText(@"\x0.x0" + Environment.NewLine);
            //richTextBoxSeeds.AppendText(@"\x1.ax1" + Environment.NewLine);
            //richTextBoxSeeds.AppendText(@"\x2.bx2" + Environment.NewLine);
            //richTextBoxSeeds.AppendText(@"\x3.\x4.x3" + Environment.NewLine);

            int NumSeeds = int.Parse(textBoxNumSeeds.Text);
            int SeedMaxLength = int.Parse(textBoxSeedMaxLength.Text);

            string[] seeds = new string[NumSeeds];

            for (int i = 0; i < NumSeeds; i++ )
            {
                seeds[i] = LambdaReactor.Reactor.randomExp(SeedMaxLength);
            }
            richTextBoxSeeds.Lines = seeds;
            richTextBoxPop.Clear();
        }

        private void labelDisplay_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(LabelDisplayBuffer, 0, 0, LabelDisplayBuffer.Width, LabelDisplayBuffer.Height);
        }

        private void button1_Click(object sender, EventArgs e)
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
                    G.DrawString(ComponentNames[i], f, ForeColor, GraphPosition.X + 20, y);
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

        public Experiment(int populationSize, int reactions, int reactionDelay, int productMaxLength, int seedMaxLength, bool copyAllowed, bool perturbations, int perturbationsCollisions, int perturbationsObjects, string[] seeds, LogUpdateDelegate logUpdate, Graphics displayG, Bitmap displayBuffer, Color backColor)
        {
            PopulationSize = populationSize;
            Reactions = reactions;
            ReactionDelay = reactionDelay;
            ProductMaxLength = productMaxLength;
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
            GAverageLength = new Graph2D(3, DisplayBufferG, new RectangleF(240, 10, 220, 170), BackColor, Color.White);
            GAverageLength.SetComponent(0, "Min length", Color.Gray);
            GAverageLength.SetComponent(1, "Mean length", Color.Silver);
            GAverageLength.SetComponent(2, "Max length", Color.White);
            GDiversity = new Graph2D(1, DisplayBufferG, new RectangleF(470, 10, 220, 170), BackColor, Color.White);
            GDiversity.SetComponent(0, "Diversity", Color.White);
            GLengthHistogram = new GraphHistogram("Length distribution", 15, DisplayBufferG, new RectangleF(700, 10, 220, 170), BackColor, Color.White, Color.LightGray);

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
            }
        }

        public void RunThread()
        {
            LogUpdate("Initializing reactor", null);

            Pop = Reactor.initPop(PopulationSize, Seeds);

            for (int r = 1; r <= Reactions; r++)
            {
                LogUpdate("New collision", Pop);
                Pop = Reactor.run(Pop, 1, ProductMaxLength, CopyAllowed);


                if (r % PerturbationsCollisions == 0)
                    if (Perturbations)
                    {
                        LogUpdate("Perturbation", null);
                        Pop = Reactor.perturb(Pop, PerturbationsObjects, SeedMaxLength);
                    }

                AverageLength = 0;
                MaxLength = 0;
                MinLength = double.MaxValue;
                int uniques = 0;
                bool unique;
                List<float> HistogramLength = new List<float>();
                for (int i = 0; i < Pop.Length; i++)
                {
                    AverageLength += Pop[i].Length;
                    if (Pop[i].Length >= MaxLength)
                        MaxLength = Pop[i].Length;
                    if (Pop[i].Length <= MinLength)
                        MinLength = Pop[i].Length;

                    unique = true;
                    for (int j = 0; j < Pop.Length; j++)
                    {
                        if (i != j && Pop[i].CompareTo(Pop[j]) == 0)
                        {
                            unique = false;
                            break;
                        }
                    }
                    if (unique)
                        uniques++;

                    HistogramLength.Add((float)Pop[i].Length);
                }
                Diversity = (double) uniques / (double) PopulationSize;
                AverageLength /= (double)Pop.Length;

                GAverageLength.AddValue(0, (float) MinLength);
                GAverageLength.AddValue(1, (float) AverageLength);
                GAverageLength.AddValue(2, (float) MaxLength);
                GDiversity.AddValue(0, (float)Diversity);

                GLengthHistogram.SetValues(HistogramLength.ToArray());

                DrawStatus(r, (double)r / (double)Reactions);
                
                Thread.Sleep(ReactionDelay);
            }
            
            LogUpdate("Reactor stopped", null);
        }

        private void DrawStatus(int reaction, double percentage)
        {
            DisplayBufferG.Clear(Background);
            DisplayBufferG.DrawString("Reaction", F12, Brushes.White, 0, 0);
            DisplayBufferG.DrawString(percentage.ToString("n0") + "%", F10, Brushes.White, 0, 24);
            DisplayBufferG.DrawString(reaction.ToString(), F32, Brushes.White, 90, -10);
            DisplayBufferG.FillRectangle(Brushes.DimGray, 4, 56, 210, 8);
            DisplayBufferG.FillRectangle(Brushes.Gainsboro, 4, 56, (float)(210.0 * percentage), 8);
            if (reaction > 0)
            {
                DisplayBufferG.DrawString("Min length: " + MinLength.ToString("f2"), F10, Brushes.White, 0, 94);
                DisplayBufferG.DrawString("Mean length: " + AverageLength.ToString("f2"), F10, Brushes.White, 0, 109);
                DisplayBufferG.DrawString("Max length: " + MaxLength.ToString("f2"), F10, Brushes.White, 0, 124);
                DisplayBufferG.DrawString("Population diversity: " + Diversity.ToString("f2"), F10, Brushes.White, 0, 139);

                GAverageLength.Draw();
                GDiversity.Draw();
                GLengthHistogram.Draw();
            }
            DisplayG.DrawImage(DisplayBuffer, 0, 0, DisplayBuffer.Width, DisplayBuffer.Height);
        }



    }
}
