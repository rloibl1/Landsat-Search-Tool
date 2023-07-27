using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LandsatSearchTool
{

    public partial class Form1 : Form
    {
        public class Scene
        {
            public string entityID;
            public string acquistionDate;
            public float cloudCover;
            public string processingLevel;
            public int path;
            public int row;
            public float minLat;
            public float minLon;
            public float maxLat;
            public float maxLon;
            public string url;
        }

        public class Airport
        {
            public string name;
            public float lat;
            public float lon;
            public int alt;
            public string type; //For use with the new airport list that has airport sizes
        }

        public class Airport_and_Scene
        {
            public string entityID;
            public string acquistionDate;
            public float cloudCover;
            public string processingLevel;
            public int path;
            public int row;
            public float minLat;
            public float minLon;
            public float maxLat;
            public float maxLon;
            public string url;
            public string type;
            public string name;
            public float lat;
            public float lon;
            public int alt;
            public char downloaded;
        }

        public class Coordinate
        {
            public float latitude;
            public float longitude;

            public Coordinate(float coord_latitude, float coord_longitude)
            {
                latitude = coord_latitude;
                longitude = coord_longitude;
            }
        }

        public class File_Info
        {
            public string name;
            public long size;
        }

        List<Scene> landsat_scenes = new List<Scene>();
        List<Airport> airports = new List<Airport>();
        List<Airport_and_Scene> airport_scenes = new List<Airport_and_Scene>();
        Dictionary<int, int> minLatMarker = new Dictionary<int, int>();

        public Form1()
        {
            bool imported = false;

            InitializeComponent();

            //Pull data from source files

            imported = Import_Premade();
            //CheckMetadata(" ", " ");
            if (imported == false)
            {
                Import_Scene();
                Import_Airport();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Match Airports with landsat scenes
            Search();
            Save();
            //Print();
        }

        private bool Import_Premade()
        {
            //String path = @"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\Landsat Airport Scene Lists\large_airport_scenes.csv";
            String path = @"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\Landsat Airport Scene Lists\large_airport_scenes_meta.csv";

            if (File.Exists(path))
            {
                var reader = new StreamReader(File.OpenRead(path));

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    Airport_and_Scene scene_match = new Airport_and_Scene();
                    //Merge the metadata from the aiport and the corresponding scene and then add to the airport scenes list
                    scene_match.name = values[0];
                    scene_match.type = values[1];
                    scene_match.lat = Convert.ToSingle(values[2]);
                    scene_match.lon = Convert.ToSingle(values[3]);
                    scene_match.alt = Convert.ToInt32(values[4]);
                    scene_match.entityID = values[5];
                    scene_match.acquistionDate = values[6];
                    scene_match.cloudCover = Convert.ToSingle(values[7]);
                    scene_match.processingLevel = values[8];
                    scene_match.path = Convert.ToInt32(values[9]);
                    scene_match.row = Convert.ToInt32(values[10]);
                    scene_match.minLat = Convert.ToSingle(values[11]);
                    scene_match.minLon = Convert.ToSingle(values[12]);
                    scene_match.maxLat = Convert.ToSingle(values[13]);
                    scene_match.maxLon = Convert.ToSingle(values[14]);
                    scene_match.url = values[15];
                    scene_match.downloaded = Convert.ToChar(values[16]);

                    airport_scenes.Add(scene_match);
                }
                return true;
            }
            return false;
        }

        private void Import_Scene()
        {
            var reader = new StreamReader(File.OpenRead(@"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\scene_list"));

            reader.ReadLine(); //Dump the first line since its just a header

            while (!reader.EndOfStream)
            {
                Scene single_scene = new Scene();
                var line = reader.ReadLine();
                var values = line.Split(',');

                single_scene.entityID = values[0];
                single_scene.acquistionDate = values[1];
                single_scene.cloudCover = Convert.ToSingle(values[2]);
                single_scene.processingLevel = values[3];
                single_scene.path = Convert.ToInt32(values[4]);
                single_scene.row = Convert.ToInt32(values[5]);
                single_scene.minLat = Convert.ToSingle(values[6]);
                single_scene.minLon = Convert.ToSingle(values[7]);
                single_scene.maxLat = Convert.ToSingle(values[8]);
                single_scene.maxLon = Convert.ToSingle(values[9]);
                single_scene.url = values[10];

                landsat_scenes.Add(single_scene);
            }
            //Close resource
            reader.Close();

            //Sort by minimum latitude for faster matching later            
            landsat_scenes.Sort((a, b) => a.minLat.CompareTo(b.minLat));

            //Build a reference table for minimum latitudes
            int prevLat = -91; //-90 is the smallest possible latitude 
            int newLat = 0;

            for (int i = 0; i < landsat_scenes.Count; i++)
            {

                newLat = (int)landsat_scenes[i].minLat; //need to check if i should do a ceiling conversion for negative latitudes

                if (newLat > prevLat)
                {
                    minLatMarker.Add(newLat, i); //Starting point for every unqiue latitude
                    prevLat = newLat;
                }

            }

        }

        private void Import_Airport()
        {
            var reader = new StreamReader(File.OpenRead(@"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\Airport Source Files\Large Airports.csv"));

            reader.ReadLine(); //Dump the first line since its just a header

            while (!reader.EndOfStream)
            {
                Airport single_airport = new Airport();
                var line = reader.ReadLine();
                var values = line.Split(',');

                try
                {
                    single_airport.type = values[1];
                    single_airport.name = values[2];
                    single_airport.lat = Convert.ToSingle(values[3]);
                    single_airport.lon = Convert.ToSingle(values[4]);
                    single_airport.alt = Convert.ToInt32(values[5]);
                    airports.Add(single_airport);
                }
                catch
                {
                    Console.WriteLine("Error");
                }
            }
            //Close resource
            reader.Close();
        }

        private void Search()
        {
            int latStart;
            int airportLat;

            //airports.Count
            for (int i = 0; i < airports.Count; i++)
            {
                airportLat = (int)airports[i].lat; //Convert from float to int (Takes the floor)
                minLatMarker.TryGetValue(airportLat, out latStart); //Find the starting index for this latitude

                //Search for all matching latitudes and add to a list
                List<Scene> lat_results = new List<Scene>();

                //Search from the cached latitude index until the latitude leaves the scene(maxLat)
                while (latStart != landsat_scenes.Count && landsat_scenes[latStart].minLat <= airports[i].lat)
                {

                    //If the airport latitude is between the min and max of the scene add it to the list
                    if ((landsat_scenes[latStart].minLat <= airports[i].lat) && (airports[i].lat <= landsat_scenes[latStart].maxLat))
                    {
                        lat_results.Add(landsat_scenes[latStart]); //Add the scene to the latitude results list
                    }

                    latStart++;
                }

                //Search through the matching latitude list for matching longitudes  
                for (int j = 0; j < lat_results.Count; j++)
                {
                    //If the airport is a large airport
                    if (airports[i].type == "large_airport")
                    {
                        //If the airport longitude is between the min and max of the scene add it to the list
                        if ((lat_results[j].minLon <= airports[i].lon) && (airports[i].lon <= lat_results[j].maxLon))
                        {
                            if (lat_results[j].cloudCover < 15) // 15% cloud cover or less
                            {
                                //Conduct a further check making sure the airport resides inside the actual picture, not in the black border
                                //if (CheckMetadata(lat_results[j].url, lat_results[j].entityID))
                                //{
                                //*Airport Scene Found*
                                Airport_and_Scene scene_match = new Airport_and_Scene();
                                //Merge the metadata from the aiport and the corresponding scene and then add to the airport scenes list
                                scene_match.entityID = lat_results[j].entityID;
                                scene_match.acquistionDate = lat_results[j].acquistionDate;
                                scene_match.cloudCover = lat_results[j].cloudCover;
                                scene_match.processingLevel = lat_results[j].processingLevel;
                                scene_match.path = lat_results[j].path;
                                scene_match.row = lat_results[j].row;
                                scene_match.minLat = lat_results[j].minLat;
                                scene_match.minLon = lat_results[j].minLon;
                                scene_match.maxLat = lat_results[j].maxLat;
                                scene_match.maxLon = lat_results[j].maxLon;
                                scene_match.url = lat_results[j].url;
                                scene_match.type = airports[i].type;
                                scene_match.name = airports[i].name;
                                scene_match.lat = airports[i].lat;
                                scene_match.lon = airports[i].lon;
                                scene_match.alt = airports[i].alt;
                                scene_match.downloaded = 'N';

                                airport_scenes.Add(scene_match);
                                //}
                            }
                        }
                    }
                }
            }
        }

        private bool CheckMetadata(string url, string id)
        {
            string url_test = @"http://landsat-pds.s3.amazonaws.com/L8/139/045/LC81390452014295LGN00/LC81390452014295LGN00_MTL.txt";
            string metadata;
            using (WebClient wc = new WebClient())
            {
                //Build the complete url for the metadata
                /*
                StringBuilder sb1 = new StringBuilder();
                sb1.Append(url);
                sb1.Remove(sb1.Length - 11, 11);
                sb1.Append("/");
                sb1.Append(id);
                sb1.Append("_MTL.txt");

                Console.WriteLine(sb1.ToString());
                */
                //string metadata = wc.DownloadString(new System.Uri(sb1.ToString()));
                metadata = wc.DownloadString(new System.Uri(url_test));
            }

            //Console.WriteLine(metadata);

            string[] metadata_rows = metadata.Split('\n');
            string[] points = new string[8];

            for (int i = 22; i < 30; i++)
            {
                string[] temp = metadata_rows[i].Split(' ');
                points[i - 22] = temp[6];
            }

            Coordinate UL = new Coordinate(Convert.ToSingle(points[0]), Convert.ToSingle(points[1]));
            Coordinate UR = new Coordinate(Convert.ToSingle(points[2]), Convert.ToSingle(points[3]));
            Coordinate LL = new Coordinate(Convert.ToSingle(points[4]), Convert.ToSingle(points[5]));
            Coordinate LR = new Coordinate(Convert.ToSingle(points[6]), Convert.ToSingle(points[7]));

            return true;
        }

        private void Save()
        {
            var csv = new StringBuilder();
            string filePath = @"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\Landsat Airport Scene Lists\large_airport_scenes_meta_new.csv";

            for (int i = 0; i < airport_scenes.Count; i++)
            {
                csv.Append(airport_scenes[i].name);
                csv.Append(',');
                csv.Append(airport_scenes[i].type);
                csv.Append(',');
                csv.Append(airport_scenes[i].lat);
                csv.Append(',');
                csv.Append(airport_scenes[i].lon);
                csv.Append(',');
                csv.Append(airport_scenes[i].alt); ;
                csv.Append(',');
                csv.Append(airport_scenes[i].entityID);
                csv.Append(',');
                csv.Append(airport_scenes[i].acquistionDate);
                csv.Append(',');
                csv.Append(airport_scenes[i].cloudCover);
                csv.Append(',');
                csv.Append(airport_scenes[i].processingLevel);
                csv.Append(',');
                csv.Append(airport_scenes[i].path);
                csv.Append(',');
                csv.Append(airport_scenes[i].row);
                csv.Append(',');
                csv.Append(airport_scenes[i].minLat);
                csv.Append(',');
                csv.Append(airport_scenes[i].minLon);
                csv.Append(',');
                csv.Append(airport_scenes[i].maxLat);
                csv.Append(',');
                csv.Append(airport_scenes[i].maxLon);
                csv.Append(',');
                csv.Append(airport_scenes[i].url);
                csv.Append(',');
                csv.Append(airport_scenes[i].downloaded);
                csv.Append(Environment.NewLine);
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        private void download(int number, int start, int interval)
        {
            bool new_scene_combination = true;
            int valid_scene = 0;
            bool skip; //Controls whether a folder is made and files are downloaded

            for (int i = start; i < start + number; i++)
            {
                skip = false; //Create a folder and download file

                using (WebClient wc = new WebClient())
                {
                    
                    //First check if this is a new Airport & Path/Row Combination
                    new_scene_combination = isNewScene(i * interval, start);
                    
                    //Build the complete url
                    StringBuilder sb1 = new StringBuilder();
                    sb1.Append(airport_scenes[i * interval].url);
                    sb1.Remove(sb1.Length - 11, 11);
                    sb1.Append("/");
                    sb1.Append(airport_scenes[i * interval].entityID);
                    //sb1.Append("_B4.TIF"); //This can change based on the desired band, pull in from GUI
                    sb1.Append("_MTL.txt");

                    //Console.WriteLine(sb1.ToString());

                    
                    //If this is a new combination then run the polygon test to verify that an airport is actually in the scene
                    if (new_scene_combination)
                    {
                        valid_scene = Polygon(sb1.ToString(), i * interval);

                        if (valid_scene == 0)
                        {
                            //Check if there are more invalid scenes of the same type
                            int j = (i * interval) + 1;
                            while (!isNewScene(j, start))
                            {
                                j++;
                            }

                            //Mark all invlaid scenes for deletion
                            for (int x = i; x < j; x++)
                            {
                                airport_scenes[x].name = "delete";
                            }

                            i = j - 1; //Start after the invalid scenes on next loop
                            skip = true; //Don't create a folder or download
                        }
                        else if (valid_scene == 3)
                        {
                            airport_scenes[i * interval].downloaded = 'E';
                            skip = true; //Don't create a folder or download
                        }
                    }
                    
                    if (!skip)
                    {
                        //Directory
                        StringBuilder sb2 = new StringBuilder();
                        sb2.Append(@"F:\Landsat Scenes\");
                        sb2.Append(airport_scenes[i * interval].entityID);

                        //Console.WriteLine(sb2.ToString());
                        System.IO.Directory.CreateDirectory(sb2.ToString()); //Creates folder for the scene

                        /*
                        //Filename  (Image)
                        sb2.Append(@"\");
                        sb2.Append(airport_scenes[i * interval].entityID);
                        sb2.Append("_B4.TIF");
                        */

                        //Filename (Metadata)
                        sb2.Append(@"\");
                        sb2.Append(airport_scenes[i * interval].entityID);
                        sb2.Append("_MTL.txt");

                        //Console.WriteLine(sb2.ToString());
                        
                        wc.DownloadFileAsync(new System.Uri(sb1.ToString()), sb2.ToString());
                        wc.DownloadFileCompleted += DownloadCompleted;

                        //Mark these scenes as downloaded in the scene listing
                        airport_scenes[i * interval].downloaded = 'Y';

                        //Save the changes to the scene list
                        Save();
                    }
                }
            }
        }

        //Only works if the interval is 1
        private bool isNewScene(int index, int start)
        {
            if (index == start)
            {
                return true;
            }

            //Previous Airport
            string current_airport = airport_scenes[index - 1].name; //airport;
            int current_path = airport_scenes[index - 1].path; //path;
            int current_row = airport_scenes[index - 1].row;
            //New Airport
            string new_airport = airport_scenes[index].name; //airport
            int new_path = airport_scenes[index].path; //path
            int new_row = airport_scenes[index].row; //row

            if (current_airport == new_airport && current_path == new_path && current_row == new_row)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        //This function checks if a point is contained within a polygon
        private int Polygon(string url, int scene_num)
        {
            Bitmap bmp;

            WebRequest request = WebRequest.Create(url);
            try
            {
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                bmp = new Bitmap(responseStream);
            }
            catch
            {
                return 3; //Web-error
            }

            Point[] vertices = findVertices(bmp); //Polygon vertices (as pixel locations)

            //Convert Airport Lat/Lon into a pixel location in the scene
            int xOffset = vertices[0].X;
            int yOffset = vertices[1].Y;

            //Airport Lat/Lon
            double airLat = airport_scenes[scene_num].lat;
            double airLon = airport_scenes[scene_num].lon;

            //Scene Lat/lon
            double minLat = airport_scenes[scene_num].minLat;
            double maxLat = airport_scenes[scene_num].maxLat;
            double minLon = airport_scenes[scene_num].minLon;
            double maxLon = airport_scenes[scene_num].maxLon;

            //Calculate x & y % for the airport lat/lon
            double xPercent = (airLon - minLon) / (maxLon - minLon);
            double yPercent = 1 - ((airLat - minLat) / (maxLat - minLat));

            //Create Airport Location Point
            int x = Convert.ToInt32(bmp.Width * xPercent) + xOffset;
            int y = Convert.ToInt32(bmp.Height * yPercent) + yOffset;

            Point airport = new Point(x, y);

            bmp.Dispose(); //Release Resource

            //Check if the airport is within the polygon
            int i, j = 0;
            bool c = false;
            int nvert = 4;//Number of vertices is 4

            //Odd number of intersections means a point is within the polygon, even is outside
            //May need to change these all to floating point intsead of integers
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((vertices[i].Y > airport.Y) != (vertices[j].Y > airport.Y)) &&
                 (airport.X < (vertices[j].X - vertices[i].X) * (airport.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                    c = !c;
            }

            //True if airport is within, false if airport is outside
            if (c)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        //Returns 4 points of a polygon in clockwise order starting at the lower left
        private Point[] findVertices(Bitmap bmp)
        {
            //Edge Detection Code
            Boolean edge_found;
            Point lower_left;
            Point upper_left;
            Point lower_right;
            Point upper_right;
            int x = 0;
            int y = 0;

            //Lower Left
            x = 0;
            edge_found = false;

            while (!edge_found)
            {
                for (y = 0; y < bmp.Height; y++)
                {
                    Color px = bmp.GetPixel(x, y);

                    if (px.R != 0 || px.G != 0 || px.B != 0)
                    {
                        //Console.WriteLine(y + ": " + px);
                        edge_found = true;
                        break;
                    }
                }
                x++;
            }

            lower_left = new Point(x, y);

            //Upper Right
            x = bmp.Width - 1;
            edge_found = false;

            while (!edge_found)
            {
                for (y = 0; y < bmp.Height; y++)
                {
                    Color px = bmp.GetPixel(x, y);

                    if (px.R != 0 || px.G != 0 || px.B != 0)
                    {
                        //Console.WriteLine(y + ": " + px);
                        edge_found = true;
                        break;
                    }
                }
                x--;
            }

            upper_right = new Point(x, y);

            //Upper Left
            y = 0;
            edge_found = false;

            while (!edge_found)
            {
                for (x = 0; x < bmp.Width; x++)
                {
                    Color px = bmp.GetPixel(x, y);

                    if (px.R != 0 || px.G != 0 || px.B != 0)
                    {
                        //Console.WriteLine(y + ": " + px);
                        edge_found = true;
                        break;
                    }
                }
                y++;
            }

            upper_left = new Point(x, y);

            //Lower Right
            y = bmp.Height - 1;
            edge_found = false;

            while (!edge_found)
            {
                for (x = 0; x < bmp.Width; x++)
                {
                    Color px = bmp.GetPixel(x, y);

                    if (px.R != 0 || px.G != 0 || px.B != 0)
                    {
                        //Console.WriteLine(y + ": " + px);
                        edge_found = true;
                        break;
                    }
                }
                y--;
            }

            lower_right = new Point(x, y);

            //All Points Found (Print them out) 
            Console.WriteLine("Lower Left: " + lower_left);
            Console.WriteLine("Upper Left: " + upper_left);
            Console.WriteLine("Upper Right: " + upper_right);
            Console.WriteLine("Lower Right: " + lower_right);

            Point[] vertices = { lower_left, upper_left, upper_right, lower_right };

            return vertices;
        }

        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            progressBar1.Value += 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int number = 9387; //how many scenes to download
            int start = 0; //starting row in excel
            int interval = 1;

            //ProgressBar Startup
            progressBar1.Minimum = 0;
            progressBar1.Maximum = number + 1;

            download(number, start, interval);
        }

        //File Information
        private void button3_Click(object sender, EventArgs e)
        {
            string directoryPath = @"F:\Landsat Scenes\";
            List<File_Info> files = new List<File_Info>();

            //Get all subdirectories
            var directories = new List<string>(Directory.GetDirectories(directoryPath));

            for (int i = 0; i < directories.Count; i++)
            {
                //Grab just the entity name
                string entityID = directories[i];
                entityID = entityID.Substring(entityID.Length - 21); //21 is the length of a landsat entityID

                //Create File path
                StringBuilder sb = new StringBuilder();
                sb.Append(directories[i]);

                //This might break if there are multiple files
                var fileName = Directory.GetFiles(sb.ToString());

                FileInfo f = new FileInfo(fileName[0]);

                File_Info single_file = new File_Info();
                single_file.name = fileName[0];
                single_file.size = f.Length;

                //Console.WriteLine(single_file.name + ": " + single_file.size);

                files.Add(single_file);
            }

            var csv = new StringBuilder();
            string filePath = @"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\fileinfo.csv";

            for (int i = 0; i < files.Count; i++)
            {
                csv.Append(files[i].name);
                csv.Append(',');
                csv.Append(files[i].size);                
                csv.Append(Environment.NewLine);
            }

            File.WriteAllText(filePath, csv.ToString());
        }


        //Removes entries marked for deletion (New List)
        private void button4_Click(object sender, EventArgs e)
        {
            List<Airport_and_Scene> updated_airport_scenes = new List<Airport_and_Scene>();

            //Create New List
            for(int i = 0; i < airport_scenes.Count; i++)
            {
                if (airport_scenes[i].name != "delete")
                {
                    updated_airport_scenes.Add(airport_scenes[i]);
                }
            }

            //Clear old list
            airport_scenes.Clear();

            //Place items into the original list again for save function
            for (int i = 0; i < updated_airport_scenes.Count; i++)
            {
                airport_scenes.Add(updated_airport_scenes[i]);
            }

                Save();
        }

        //Checks to make sure all files in the list were downloaded (Verify D/L)
        private void button5_Click(object sender, EventArgs e)
        {
            //List<Airport_and_Scene> file_list = new List<Airport_and_Scene>();            
            List<File_Info> file_list = new List<File_Info>();

            //Files missing from the directory
            //Read in the list of files in the directory
            string path = @"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\fileinfo.csv";

            if (File.Exists(path))
            {
                var reader = new StreamReader(File.OpenRead(path));

                while (!reader.EndOfStream)
                {
                    //Airport_and_Scene file_name = new Airport_and_Scene();
                    File_Info file =  new File_Info();

                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    //Create a list of the files that are downloaded
                    StringBuilder sb1 = new StringBuilder();
                    sb1.Append(values[0]);
                    sb1.Remove(sb1.Length - 7, 7);
                    file.name = sb1.ToString(40, 21);
                    file.size = Convert.ToInt64(values[1]);
                    file_list.Add(file);
                }
            }

            //For every entry in the scene list, check if it was downloaded
            for(int i = 0; i < airport_scenes.Count; i++)
            {                
                int index = inFileList(file_list, airport_scenes[i].entityID);
                //If it wasn't downloaded remove it from the download list
                if (index == -1)
                {
                    Console.WriteLine(airport_scenes[i].entityID);
                    airport_scenes[i].name = "delete";
                }
                else
                {
                    //File was downloaded                    
                    if (file_list[index].size == 0)
                    {
                        airport_scenes[i].downloaded = 'N';
                    }
                    else
                    {
                        airport_scenes[i].downloaded = 'Y';
                    }
                }
            }
            //Save all of these changes
            Save();
        }

        private int inFileList(List<File_Info> file_list, string entityID)
        {
            for (int i = 0; i < file_list.Count; i++)
            {
                if (file_list[i].name == entityID)
                {
                    return i; //File was downloaded, return which file
                }
            }
            return -1; //File not downloaded
        }


        private void button6_Click(object sender, EventArgs e)
        {
            for(int i = 0; i < airport_scenes.Count - 1; i++)
            {
                if (airport_scenes[i].name != "delete")
                {
                    for (int j = i + 1; j < airport_scenes.Count; j++)
                    {
                        if (airport_scenes[i].entityID == airport_scenes[j].entityID)
                        {
                            airport_scenes[j].name = "delete";
                        }
                    }
                }                
            }
            Save();            
        }


        //Remove any corrupted files after they are downloaded
        private void button7_Click(object sender, EventArgs e)
        {
            long threshold = 37000000;

            List<File_Info> file_list = new List<File_Info>();

            //Read in the list of files in the directory
            string path = @"C:\Users\rloibl1\Documents\Homework\CSCE699 - Remote Sensing\fileinfo.csv";
            
            if (File.Exists(path))
            {
                var reader = new StreamReader(File.OpenRead(path));

                while (!reader.EndOfStream)
                {
                    //Airport_and_Scene file_name = new Airport_and_Scene();
                    File_Info file = new File_Info();

                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    //Create a list of the files that are downloaded
                    StringBuilder sb1 = new StringBuilder();
                    sb1.Append(values[0]);
                    sb1.Remove(sb1.Length - 7, 7);
                    file.name = sb1.ToString(40, 21);
                    file.size = Convert.ToInt64(values[1]);

                    if (file.size < threshold)
                    {
                        sb1.Remove(sb1.Length - 21, 21);
                        Directory.Delete(sb1.ToString(), true); //Delete folder and its contents
                        file_list.Add(file);
                    }                    
                }
            }

            //Match the corrupted files with their entry in the scene list
            for (int i = 0; i < file_list.Count; i++)
            {
                for (int j = 0; j < airport_scenes.Count; j++)
                {
                    if (file_list[i].name == airport_scenes[j].entityID)
                    {
                        airport_scenes[j].name = "delete";
                        break;
                    }
                }
            }
            //Save all of these changes
            Save();
        }
    }
}

