using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using MicrosoftInternal.AdvancedCollections;
using TerraViewer.org.worldwidetelescope.www;
using System.Threading;
namespace TerraViewer
{
    public partial class Search : TabForm
    {
        static TreeDictionary<string, IPlace> autoCompleteList = new TreeDictionary<string, IPlace>(true);

        public Search()
        {
            InitializeComponent();
            SetUiStrings();

        }

        private void SetUiStrings()
        {
            this.searchText.Text = Language.GetLocalizedText(308, "Type your search here");
            this.toolTips.SetToolTip(this.searchText, Language.GetLocalizedText(309, "Enter your search terms here."));
            this.raLabel.Text = Language.GetLocalizedText(310, "RA");
            this.decLabel.Text = Language.GetLocalizedText(311, "Dec");
            this.searchResults.AddText = Language.GetLocalizedText(161, "Add New Item");
            this.searchResults.EmptyAddText = Language.GetLocalizedText(162, "No Results");
            this.SearchView.Text = Language.GetLocalizedText(312, "Search View");
            this.wwtButton1.Text = Language.GetLocalizedText(313, "Server");
            this.plotResults.Text = Language.GetLocalizedText(314, "Plot Results");
            this.toolTips.SetToolTip(this.plotResults, Language.GetLocalizedText(316, "Show the location of each result on the sky map."));
            this.GoToRADec.Text = Language.GetLocalizedText(315, "Go");
            this.measureTool.Text = Language.GetLocalizedText(931, "Distance");
        }
        protected override void SetFocusedChild()
        {
            searchResults.Focus();
        }

        public static void AddParts(string key, IPlace place)
        {

            key = key.ToLower();
            autoCompleteList.Add(key, place);
            string[] parts = key.Split(new char[] { ' ' });
            if (parts.Length > 1)
            {
                foreach (string part in parts)
                {
                    if (!string.IsNullOrEmpty(part))
                    {
                        autoCompleteList.Add(part, place);
                    }
                }
            }
        }


        bool textChanged = false;

        private void searchText_TextChanged(object sender, EventArgs e)
        {
            textChanged = true;
            searchTimer.Enabled = true;
          
        }

        private void searchText_KeyUp(object sender, KeyEventArgs e)
        {
   
        }

        private void SearchView_Click(object sender, EventArgs e)
        {
            Coordinates[] corners = Earth3d.MainWindow.CurrentViewCorners;

            if (corners != null && !String.IsNullOrEmpty(Earth3d.MainWindow.Constellation))
            {

                IPlace[] results = ContextSearch.FindConteallationObjects(Earth3d.MainWindow.Constellation, corners, Classification.Unfiltered);
                searchResults.Clear();
                if (results != null)
                {
                    searchResults.AddRange(results);
                    searchResults.Refresh();    
                }


            }
            UpdateMarkers();

        }

        public static IPlace FindCatalogObject(string name)
        {
            InitSearchTable();
            foreach (KeyValuePair<string, IPlace> kv in autoCompleteList.StartFromKey(name.ToLower(), TraversalStartingPoint.EqualOrMore, TraversalDirection.LowToHigh))
            {
                if (kv.Key.ToLower().Replace(" ", "") == name.ToLower().Replace(" ", ""))
                {
                    if (kv.Value.StudyImageset == null)
                    {
                        return kv.Value;
                    }
                }

            }
            return null;


        }

        public static IPlace FindCatalogObjectExact(string name)
        {
            InitSearchTable();

            string shortName = name.ToLower().Replace(" ", "");

            foreach (KeyValuePair<string, IPlace> kv in autoCompleteList.StartFromKey(name.ToLower(), TraversalStartingPoint.EqualOrMore, TraversalDirection.LowToHigh))
            {
                IPlace place = kv.Value as IPlace;

                if (place != null && place.Name.ToLower().Replace(" ", "") == shortName)
                {
                    //if (kv.Value.StudyImageset == null)
                    {
                        return kv.Value;
                    }
                }

            }

            foreach (KeyValuePair<string, IPlace> kv in autoCompleteList.StartFromKey(name.ToLower(), TraversalStartingPoint.EqualOrMore, TraversalDirection.LowToHigh))
            {
                if (kv.Key.ToLower().Replace(" ", "") == shortName )
                {
                    if (kv.Value.StudyImageset == null)
                    {
                        return kv.Value;
                    }
                }

            }

            foreach (KeyValuePair<string, IPlace> kv in autoCompleteList.StartFromKey(name.ToLower(), TraversalStartingPoint.EqualOrMore, TraversalDirection.LowToHigh))
            {
                if (kv.Key.ToLower().Replace(" ", "").StartsWith(shortName))
                {
                    //if (kv.Value.StudyImageset == null)
                    {
                        return kv.Value;
                    }
                }

            }
            return null;


        }

        public static void InitSearchTable()
        {
            if (!searchTableInitialized)
            {
                LoadSearchTable();
            }
        }

        private void searchTimer_Tick(object sender, EventArgs e)
        {
            InitSearchTable();

            if (Earth3d.MainWindow.Space != plotResults.Visible)
            {
                plotResults.Visible = Earth3d.MainWindow.Space;
            }



            if ( textChanged )
            {
                if (string.IsNullOrEmpty(searchText.Text))
                {
                    searchResults.Clear();
                    textChanged = false;
                    return;
                }               
                string searchString = CleanSearchString(searchText.Text);

                
                SearchCriteria sc = new SearchCriteria(searchString);

                int length = sc.Target.Length;

                List<Object> tempList = new List<Object>();
 
                foreach (KeyValuePair<string, IPlace> kv in autoCompleteList.StartFromKey(sc.Target, TraversalStartingPoint.EqualOrMore, TraversalDirection.LowToHigh))
                {
                    if (tempList.Count > 100)
                    {
                        break;
                    }

                    if (kv.Value.Classification != Classification.Unidentified && (sc.Classification & kv.Value.Classification) == 0)
                    {
                        continue;
                    }

                    if (sc.Constellation != null && sc.Constellation != kv.Value.Constellation)
                    {
                        continue;
                    }

                    if (kv.Value.Magnitude > sc.MagnitudeMax)
                    {
                        continue;
                    }

                    if (kv.Value.Magnitude < sc.MagnitudeMin)
                    {
                        continue;
                    }

                    if (kv.Key == sc.Target || (kv.Key.Length > length && kv.Key.Substring(0, length) == sc.Target))
                    {
                        if (!tempList.Contains(kv.Value))
                        {
                            Place place = kv.Value as Place;
                            if (place != null && place.Name.ToLower() == searchString)
                            {
                                tempList.Insert(0, kv.Value);
                            }
                            else
                            {

                                tempList.Add(kv.Value);
                            }
                        }
                    }
                    else
                    {
                        int len = Math.Min(kv.Key.Length, sc.Target.Length);
                        if (string.Compare(kv.Key.Substring(0, len), sc.Target.Substring(0, len)) > 0)
                        {
                            break;
                        }
                    }
                }

                if (sc.Keywords != null && sc.Keywords.Count > 0)
                {
                    foreach (string keyword in sc.Keywords)
                    {
                        foreach (KeyValuePair<string, IPlace> kv in autoCompleteList.StartFromKey(keyword, TraversalStartingPoint.EqualOrMore, TraversalDirection.LowToHigh))
                        {
                            length = keyword.Length;

                            if (tempList.Count > 100)
                            {
                                break;
                            }

                            if (kv.Value.Classification != Classification.Unidentified && (sc.Classification & kv.Value.Classification) == 0)
                            {
                                continue;
                            }

                            if (kv.Value.Magnitude > sc.MagnitudeMax)
                            {
                                continue;
                            }

                            if (kv.Value.Magnitude < sc.MagnitudeMin)
                            {
                                continue;
                            }

                            if (sc.Constellation != null && sc.Constellation != kv.Value.Constellation)
                            {
                                continue;
                            }


                            if (kv.Key == keyword || (kv.Key.Length > length && kv.Key.Substring(0, length) == keyword))
                            {
                                if (!tempList.Contains(kv.Value))
                                {
                                    Place place = kv.Value as Place;
                                    //System.Diagnostics.Debug.WriteLine(place.Name);
                                    if (place != null && place.Name == searchString)
                                    {
                                        tempList.Insert(0, kv.Value);
                                    }
                                    else
                                    {

                                        tempList.Add(kv.Value);
                                    }
                                }
                            }
                            else
                            {
                                int len = Math.Min(kv.Key.Length, sc.Target.Length);
                                if (string.Compare(kv.Key.Substring(0, len), sc.Target.Substring(0, len)) > 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }


                searchResults.Clear();
                searchResults.AddRange( tempList);
                UpdateMarkers();
            }
            //searchTimer.Enabled = false;
            textChanged = false;

        }

        private string CleanSearchString(string searchString)
        {
            searchString = searchString.ToLower();

            if (searchString.Contains("ngc "))
            {
                for(int i =0; i<10; i++)
                {
                    searchString = searchString.Replace(string.Format("ngc {0}",i),string.Format("ngc{0}",i));
                }
            }

            if (searchString.Contains("ic "))
            {
                for(int i =0; i<10; i++)
                {
                    searchString = searchString.Replace(string.Format("ic {0}",i),string.Format("ic{0}",i));
                }
            }
            if (searchString.Contains("hr "))
            {
                for(int i =0; i<10; i++)
                {
                    searchString = searchString.Replace(string.Format("ic {0}",i),string.Format("ic{0}",i));
                }
            } 
            if (searchString.Contains("hd "))
            {
                for(int i =0; i<10; i++)
                {
                    searchString = searchString.Replace(string.Format("ic {0}",i),string.Format("ic{0}",i));
                }
            }    
            if (searchString.Contains("sao "))
            {
                for(int i =0; i<10; i++)
                {
                    searchString = searchString.Replace(string.Format("ic {0}",i),string.Format("ic{0}",i));
                }
            }            
            return searchString;
        }

        public override bool AdvanceSlide(bool fromStart)
        {
            return searchResults.ShowNext(fromStart, false);
        }

        private void searchResults_ItemClicked(object sender, Object e)
        {
            if (e is Place)
            {
                Place p = (Place)e;
                if (p.Tour != null)
                {
                    FolderBrowser.LaunchTour(p.Tour);
                    return;
                }
            }
            
            Earth3d.MainWindow.GotoTarget((IPlace)e, false, false, true);

        }

        private void searchResults_ItemDoubleClicked(object sender, Object e)
        {
            Earth3d.MainWindow.GotoTarget((IPlace)e, false, true, true);

        }

        private void searchResults_ItemHover(object sender, Object e)
        {
            if (Earth3d.MainWindow.IsWindowOrChildFocused())
            {
                this.Focus();
            }
            if (e is IPlace || e is IImageSet)
            {
                IImageSet imageset = null;
                if (e is IPlace)
                {
                    IPlace p = (IPlace)e;
                    Earth3d.MainWindow.SetLabelText(p, true);
                    if (p.BackgroundImageSet != null)
                    {
                        imageset = p.BackgroundImageSet;
                    }
                    else if (p.StudyImageset != null)
                    {
                        imageset = p.StudyImageset;
                    }
                }
                if (e is IImageSet)
                {
                    imageset = e as IImageSet;
                }

                toolTips.SetToolTip(searchResults, ((IThumbnail)e).Name);

                if (imageset != null)
                {
                    Earth3d.MainWindow.PreviewImageset = imageset;
                    Earth3d.MainWindow.PreviewBlend.TargetState = true;
                }
                else
                {
                    Earth3d.MainWindow.PreviewBlend.TargetState = false;
                }
            }
            else
            {
                if (e != null)
                {
                    toolTips.SetToolTip(searchResults, ((IThumbnail)e).Name);
                }
                Earth3d.MainWindow.SetLabelText(null, false);
                Earth3d.MainWindow.PreviewBlend.TargetState = false;

            }


            //if (e != null)
            //{
            //    IPlace p = (IPlace)e;
            //    Earth3d.MainWindow.SetLabelText(p, true);
            //    toolTips.SetToolTip(searchResults, p.Name);
            //}
            //else
            //{
            //    if (e != null)
            //    {
            //        toolTips.SetToolTip(searchResults, ((IThumbnail)e).Name);
            //    }       
            //    Earth3d.MainWindow.SetLabelText(null, false);

            //}
        }

        //private void wwtButton1_Click(object sender, EventArgs e)
        //{
        //    WWTWebService wwtWebService = new WWTWebService();
        //    AstroObjectsDataset dataset = wwtWebService.GetAstroObjectsByName(searchText.Text);
        //    string a = dataset.ToString();
        //    searchResults.Clear();

        //    foreach (AstroObjectsDataset.spGetAstroObjectsRow row in dataset.spGetAstroObjects.Rows)
        //    {
        //        IPlace place = Place.FromAstroObjectsRow(row);
        //        searchResults.Add(place);
        //    }
        //}


        private void searchText_Enter(object sender, EventArgs e)
        {
            if (searchText.Text == Language.GetLocalizedText(308, "Type your search here"))
            {
                searchText.SelectAll();
                //searchText.Text = "";
            }
            Earth3d.NoStealFocus = true;
            searchResults.DontStealFocus = true;

        }

        public string SearchStringText
        {
            get { return searchText.Text; }
        }

        private void searchText_MouseClick(object sender, MouseEventArgs e)
        {
            if (searchText.Text == Language.GetLocalizedText(308, "Type your search here"))
            {
                //searchText.SelectAll();
                searchText.Text = "";
            }
        }

        private void searchText_Leave(object sender, EventArgs e)
        {
            Earth3d.NoStealFocus = false;
        }

        private void Search_Load(object sender, EventArgs e)
        {
            searchText.Text = Language.GetLocalizedText(308, "Type your search here");
            coordinateType.Items.Add(Language.GetLocalizedText(555, "J2000"));
            coordinateType.Items.Add(Language.GetLocalizedText(556, "Alt/Az"));
            coordinateType.Items.Add(Language.GetLocalizedText(557, "Galactic"));
            coordinateType.Items.Add(Language.GetLocalizedText(558, "Ecliptic"));
            coordinateType.Items.Add(Language.GetLocalizedText(932, "Lat/Lng"));
            coordinateType.SelectedIndex = 0;
        }

        static bool searchTableInitialized = false;
        static Mutex LoadSearchMutex = new Mutex();
        static public void LoadSearchTable()
        {
            try
            {
                LoadSearchMutex.WaitOne();
                if (searchTableInitialized)
                {
                    return;
                }
                foreach (string abreviation in ContextSearch.constellationObjects.Keys)
                {
                    foreach (IPlace place in ContextSearch.constellationObjects[abreviation])
                    {
                        try
                        {
                            foreach (string name in place.Names)
                            {
                                AddParts(name, place);
                            }

                            if (place.Classification != Classification.Unidentified)
                            {
                                AddParts(place.Classification.ToString(), place);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                searchTableInitialized = true;
                LoadSearchMutex.ReleaseMutex();
            }

        }

        public void DisplaySearchResults(VoTable table)
        {
            searchResults.Clear();
            int count = 0;
            foreach(VoRow row in table.Rows)
            {
                double ra = Convert.ToDouble(row["RA"])/15;
                double dec = Convert.ToDouble(row["DEC"]);

                TourPlace pl = new TourPlace(row["id"].ToString(), dec, ra, Classification.Star, Constellations.Containment.FindConstellationForPoint(ra, dec), ImageSetType.Sky, -1);
                searchResults.Add( pl);
                if (count++ > 200)
                {
                    break;
                }
            }
            UpdateMarkers();
        }

        private void plotResults_CheckedChanged(object sender, EventArgs e)
        {
            Earth3d.MainWindow.ShowKmlMarkers = plotResults.Checked;

            UpdateMarkers();
            
        }

        private void UpdateMarkers()
        {
            if (Earth3d.MainWindow.KmlMarkers != null)
            {
                Earth3d.MainWindow.KmlMarkers.ClearPoints();
                if (plotResults.Checked)
                {
                    foreach (object o in searchResults.Items)
                    {
                        IPlace p = (IPlace)o;
                        Earth3d.MainWindow.KmlMarkers.AddPoint(p.Name, p.RA, p.Dec);
                    }
                }
            }
        }

        private void searchResults_ItemContextMenu(object sender, object e)
        {
            Point pntClick = Cursor.Position;

            if (e is IPlace)
            {
                Earth3d.MainWindow.ShowContextMenu((IPlace)e, Earth3d.MainWindow.PointToClient(Cursor.Position), false, true);
            }
        }

        private void GoToRADec_Click(object sender, EventArgs e)
        {
            int index = coordinateType.SelectedIndex;


            double ra = 0;
            double dec = 0;
            bool raValid = false;
            bool decValid = false;
            switch (index)
            {
                case 0: // Equitorial
                    {
                        ra = Coordinates.ParseRA(raText.Text, false);
                        dec = Coordinates.ParseDec(decText.Text);
                        raValid = Coordinates.ValidateRA(raText.Text);
                        decValid = Coordinates.ValidateDec(decText.Text);
                    }
                    break;
                case 2: // Galactic
                    {
                        double l = Coordinates.Parse(raText.Text);
                        double b = Coordinates.ParseDec(decText.Text);
                        raValid = Coordinates.Validate(raText.Text);
                        decValid = Coordinates.ValidateDec(decText.Text);
                        if (raValid && decValid)
                        {

                            double[] result = Earth3d.GalactictoJ2000(l, b);
                            ra = result[0] / 15;
                            dec = result[1];
                        }
                    }
                    break;
                case 3: // Ecliptic
                    {
                        double l = Coordinates.Parse(raText.Text);
                        double b = Coordinates.ParseDec(decText.Text);
                        raValid = Coordinates.Validate(raText.Text);
                        decValid = Coordinates.ValidateDec(decText.Text);
                        if (raValid && decValid)
                        {

                            AstroCalc.AstroRaDec radec = AstroCalc.AstroCalc.EclipticToJ2000(l, b, SpaceTimeController.JNow);
                            ra = radec.RA;
                            dec = radec.Dec;
                        }
                    }
                    break;
                case 4: // Geo
                    {
                        ra = -Coordinates.Parse(raText.Text)/15;
                        dec = Coordinates.ParseDec(decText.Text);
                        raValid = Coordinates.Validate(raText.Text);
                        decValid = Coordinates.ValidateDec(decText.Text);
                     
                    }
                    break;     
                case 1: // alt/az
                    {
                        double az = Coordinates.Parse(raText.Text);
                        double alt = Coordinates.ParseDec(decText.Text);
                        raValid = Coordinates.Validate(raText.Text);
                        decValid = Coordinates.ValidateDec(decText.Text);
                        Coordinates radec= Coordinates.HorizonToEquitorial(Coordinates.FromLatLng(alt, az), SpaceTimeController.Location, SpaceTimeController.Now);
                        ra = radec.RA;
                        dec = radec.Dec;
                    }
                    break;
            }
            if (raValid && decValid)
            {
                if (Earth3d.MainWindow.SolarSystemMode)
                {
                    Vector3d pnt = Coordinates.GeoTo3dDouble(dec, Coordinates.Parse(raText.Text));


                    pnt = Vector3d.TransformCoordinate(pnt, Planets.EarthMatrix);
                    pnt.Normalize();
                    Vector2d radec = Coordinates.CartesianToLatLng(pnt);

                    Earth3d.MainWindow.TargetLat = radec.Y;
                    Earth3d.MainWindow.TargetLong = radec.X - 90;

                }
                else
                {

                    Earth3d.MainWindow.GotoTargetRADec(ra, dec, true, false);
                }
            }
        }

        private void raText_Enter(object sender, EventArgs e)
        {
            searchResults.DontStealFocus = true;
            Earth3d.NoStealFocus = true;

        }

        private void decText_Enter(object sender, EventArgs e)
        {
            searchResults.DontStealFocus = true;
            Earth3d.NoStealFocus = true;

        }

        private void searchResults_Enter(object sender, EventArgs e)
        {
            searchResults.DontStealFocus = false;
            Earth3d.NoStealFocus = false;
        }

        private void raText_Validating(object sender, CancelEventArgs e)
        {
            bool valid = false;

            switch (coordinateType.SelectedIndex)
            {
                case 0:
                    valid = Coordinates.ValidateRA(raText.Text);
                    break;
                case 1:
                    valid = Coordinates.Validate(raText.Text);
                    break;
                case 4:
                case 3:
                case 2:
                    valid = Coordinates.Validate(raText.Text);
                    break;
            }

            if (valid)
            {
                raText.BackColor = searchText.BackColor;
            }
            else
            {
                raText.BackColor = Color.Red;
            }
        }

        private void decText_Validating(object sender, CancelEventArgs e)
        {
            bool valid = Coordinates.ValidateDec(decText.Text);

            if (valid)
            {
                decText.BackColor = searchText.BackColor;
            }
            else
            {
                decText.BackColor = Color.Red;
            }
        }

        private void Search_Shown(object sender, EventArgs e)
        {
            searchText.Focus();
        }

        private void coordinateType_SelectionChanged(object sender, EventArgs e)
        {
            int index = coordinateType.SelectedIndex;
            switch (index)
            {
                case 0: // RA-DEC
                    this.raLabel.Text = Language.GetLocalizedText(310, "RA");
                    this.decLabel.Text = Language.GetLocalizedText(311, "Dec");
                    break;
                case 1: // alt-az
                    this.raLabel.Text = Language.GetLocalizedText(268, "Az :");
                    this.decLabel.Text = Language.GetLocalizedText(269, "Alt : ");
                    break;
                case 2: // Galactic
                    this.raLabel.Text = "\u03BB";
                    this.decLabel.Text = "\u03B2";
                    break;
                case 3: // Ecliptic
                    this.raLabel.Text = "\u03BB";
                    this.decLabel.Text = "\u03B2";
                    break;
                case 4: // Ecliptic
                    this.raLabel.Text = Language.GetLocalizedText(950, "Lng");
                    this.decLabel.Text = Language.GetLocalizedText(951, "Lat");
                    break;
            }
        }

        private void measureTool_Click(object sender, EventArgs e)
        {
            Earth3d.MainWindow.Measuring = !Earth3d.MainWindow.Measuring;
        }
    }

    public class SearchCriteria
    {
        public SearchCriteria(string searchString)
        {
            searchString = searchString.Trim().ToLower();
            Target = searchString;
            MagnitudeMin = -100.0;
            MagnitudeMax = 100.0;

            
            

            List<string> keywords = new List<string>(searchString.ToLower().Split(new char[] { ' ' }));
            if (keywords.Count > 1)
            {
                for (int i = keywords.Count - 1; i > -1; i--)
                {
                    if (keywords[i] == ">" && i > 0 && i < keywords.Count - 1)
                    {
                        if (keywords[i - 1] == "m" || keywords[i - 1] == "mag" || keywords[i - 1] == "magnitude")
                        {
                            try
                            {
                                MagnitudeMin = Convert.ToDouble(keywords[i + 1]);
                            }
                            catch
                            {
                            }
                            keywords.RemoveAt(i + 1);
                            keywords.RemoveAt(i);
                            keywords.RemoveAt(i - 1);
                            i -= 2;
                            continue;
                        }
                    }

                    if (keywords[i] == "<" && i > 0 && i < keywords.Count - 2)
                    {
                        if (keywords[i - 1] == "m" || keywords[i - 1] == "mag" || keywords[i - 1] == "magnitude")
                        {
                            try
                            {
                                MagnitudeMax = Convert.ToDouble(keywords[i + 1]);
                            }
                            catch
                            {
                            }
                            keywords.RemoveAt(i + 1);
                            keywords.RemoveAt(i );
                            keywords.RemoveAt(i - 1);
                            i-=2;
                            continue;
                        }
                    }
                    bool brokeOut = false;

                    foreach (string classId in Enum.GetNames(typeof(Classification)))
                    {
                        if (keywords[i] == classId.ToLower())
                        {
                            Classification |= (Classification)Enum.Parse(typeof(Classification), classId);
                            keywords.RemoveAt(i);
                            brokeOut = true;
                            break;
                        }

                    }

                    if (brokeOut)
                    {
                        continue;
                    }

                    if (Constellations.Abbreviations.ContainsKey(keywords[i].ToUpper()))
                    {
                        Constellation = keywords[i].ToUpper();
                        keywords.RemoveAt(i);
                        continue;
                    }

                    if (Constellations.FullNames.ContainsKey(keywords[i].ToUpper()))
                    {
                        Constellation = Constellations.Abbreviation(keywords[i].ToUpper());
                        keywords.RemoveAt(i);
                        continue;
                    }
                }
                //keywords.Add(searchString);
                Keywords = keywords;
                string spacer = "";
                Target = "";
                foreach (string keyword in Keywords)
                {
                    Target += spacer + keyword;
                    spacer = " ";
                }
            }
            else
            {
                Keywords = null;
            }

            if (Classification == 0)
            {
                Classification = Classification.Unfiltered;
            }
        }
        public string Target;
        public string Constellation;
        public List<string> Keywords;
        public Classification Classification = 0;
        public double MagnitudeMin;
        public double MagnitudeMax;
    }
}