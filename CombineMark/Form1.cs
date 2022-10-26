using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Tekla.Structures.Drawing;
using tsm = Tekla.Structures.Model;
using Tekla.Structures.Drawing.UI;

namespace CombineMark
{
    public partial class Form1 : Form
    {
        tsm.Model _model = new tsm.Model();
        DrawingHandler _drawinghandler = new DrawingHandler();
        List<Mark> _allmarksMK = new List<Mark>();
        List<string> _allmarksStr = new List<string>();
        List<BaseMark> Basemarks = new List<BaseMark>();
        ArrayList ObjectsToSelect = new ArrayList();
        ViewBase view = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (!_model.GetConnectionStatus() || !_drawinghandler.GetConnectionStatus())
                    return;

                var drawingObjects = _drawinghandler.GetDrawingObjectSelector().GetSelected();

                foreach (var obj in drawingObjects)
                {
                    if (obj is Mark mark)
                    {
                        DrawingObjectEnumerator mo = mark.GetRelatedObjects(new[] { typeof(Part) });
                        while (mo.MoveNext())
                        {
                            Basemarks.Add(MarkEnumerator(mark));
                        }
                    }
                }

                List<string> al = new List<string>();
                foreach (var b in Basemarks)
                {
                    al.Add(b.StrMark);
                }

                Picker picker = _drawinghandler.GetPicker();
                DrawingObject finobj = null;
                ViewBase view = null;

                string prompt = "Pick mark";
                picker.PickObject(prompt, out finobj, out view);
                Mark mymark = (Mark)finobj;
                Combine(mymark, Basemarks);
                this.Close();

            }
            catch
            {
                this.Close();
            }

        }

        public BaseMark MarkEnumerator(Mark mark)
        {
            DrawingObjectEnumerator mo = mark.GetRelatedObjects(new[] { typeof(Part) });
            while (mo.MoveNext())
            {
                Tekla.Structures.Drawing.Part mypart = (Tekla.Structures.Drawing.Part)mo.Current;
                Tekla.Structures.Identifier myIdentifier = mypart.ModelIdentifier;
                Tekla.Structures.Model.ModelObject ModelSideObject = new tsm.Model().SelectModelObject(myIdentifier);
                tsm.Beam partmodel = new tsm.Beam();
                partmodel.Identifier = myIdentifier;
                partmodel.Select();
                string mytext = partmodel.GetPartMark();
                return new BaseMark { Mark = mark, StrMark = mytext };
            }
            return null;
        }

        public string displayMembers(List<string> allmarks)
        {
            return string.Join(Environment.NewLine, allmarks.ToArray());
        }

        public void Combine(Mark mymark, List<BaseMark> Basemarks)
        {
            BaseMark finmark = MarkEnumerator(mymark);

            var sortedMarks = from m in Basemarks
                              group m by m.StrMark into g
                              select new { Name = g.Key, Count = g.Count() };
            List<string> al2 = new List<string>();
            foreach (var group in sortedMarks)
            {
                al2.Add($"Value: {group.Name}  Count: {group.Count}");
            }
            foreach (var group in sortedMarks)
            {                
                if (group.Name == finmark.StrMark)
                {                    
                    ElementBase[] tempMark = new ElementBase[20];
                    DrawingColors color = DrawingColors.Green;
                    double height = 2.4;
                    string fontName = "Arial";
                    mymark.Attributes.Content.CopyTo(tempMark,0);
                    mymark.Attributes.Content.Clear();                    
                    mymark.Attributes.Content.Add(new TextElement($"({ group.Count }) - ", new FontAttributes(color, height, fontName, false, false)));
                    for (int i = 0; i < tempMark.Length; i++)
                    {
                        if (tempMark[i] != null)
                            mymark.Attributes.Content.Add(tempMark[i]);                            
                    }
                    mymark.Modify();
                }
            }

            foreach (var b in Basemarks)
            {
                if (b.Mark.InsertionPoint != finmark.Mark.InsertionPoint && b.StrMark == finmark.StrMark)
                {
                    b.Mark.Delete();
                }
            }
        }

        private bool Mouse(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class BaseMark
    {
        public Mark Mark { get; set; }
        public string StrMark { get; set; }
    }
}
