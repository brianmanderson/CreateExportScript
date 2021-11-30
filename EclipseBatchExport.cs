using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.IO;
using System.Windows.Forms;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.1")]
[assembly: AssemblyFileVersion("1.0.0.1")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]

namespace EclipseBatchExport
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                using (VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication())
                {
                    Execute(app);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        static void Execute(VMS.TPS.Common.Model.API.Application app)
        {
            string patientId = "";
            string dcmType = "";
            string uidIn = "";
            string listPath = "";
            string logPath = @"O:\exportDicomTest";
            string filePath = Path.Combine(logPath, "DICOM_ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt");
            string Message;
            List<string[]> lines = new List<string[]>();
            List<string> tmplines = new List<string>();

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Title = "Select A File";
            openDialog.Filter = "Text Files (*.txt)|*.txt" + "|" +
                                "Image Files (*.png;*.jpg)|*.png;*.jpg" + "|" +
                                "All Files (*.*)|*.*";
            openDialog.InitialDirectory = logPath;
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                listPath = openDialog.FileName;
                Message = $"Selected File: {listPath}";
                WriteLog(filePath, logPath, Message);
            }
            else
            {
                Message = "Process cancelled by user";
                WriteLog(filePath, logPath, Message);
            }

            //string exportInstructions = listPath;
            using (var reader = new StreamReader(listPath)) // input data file must have the correct information
            {

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    tmplines.Add(line);

                }

                int j = 0;
                foreach (string l in tmplines)
                {
                    lines.Add(l.Split(','));

                    j++;
                }


            }
            foreach(string[] str in lines)
            {
                Patient patient = app.OpenPatientById(str[0]);
                string planName = str[1];
                WriteTxtFile(patient,planName,logPath);
                app.ClosePatient();
            }
        }


        public static void WriteTxtFile(Patient patient, string planName,string logPath)
        {
            List<Course> courses = new List<Course>();
            
            List<PlanSetup> tmpplanSetups = new List<PlanSetup>();
            List<PlanSetup> planSetups = new List<PlanSetup>();

            List<Course> target_Courses = new List<Course>();
            courses = patient.Courses.ToList();
            foreach(Course c in courses)
            {
                tmpplanSetups = c.PlanSetups.ToList();
                foreach(PlanSetup p in tmpplanSetups)
                {
                    //MessageBox.Show(p.ApprovalStatus.ToString());
                    if (p.Id == planName && p.ApprovalStatus.ToString() == "TreatmentApproved")
                    {
                        target_Courses.Add(c);
                    }
                }

            }
            foreach (Course targetCourse in target_Courses)
            {
                planSetups = courses.FirstOrDefault(x => x.Id == targetCourse.Id).PlanSetups.ToList();
                string expInstructions = string.Format("ExportInstructions.txt");
                string exportFile = System.IO.Path.Combine(logPath, expInstructions);


                //finding UID
                string[] uids = { planSetups.FirstOrDefault(x => x.Id == planName).UID.ToString(), planSetups.FirstOrDefault(x => x.Id == planName).StructureSet.UID, planSetups.FirstOrDefault(x => x.Id == planName).Dose.UID, planSetups.FirstOrDefault(x => x.Id == planName).StructureSet.Image.Series.UID };


                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                if (!File.Exists(exportFile))
                {
                    // Create a file to write to.   
                    using (StreamWriter exportWriter = File.CreateText(exportFile))
                    {
                        exportWriter.WriteLine($"{patient.Id},{uids[0]},{uids[1]},{uids[2]},{uids[3]}");
                    }
                }
                else
                {
                    using (StreamWriter exportWriter = File.AppendText(exportFile))
                    {
                        exportWriter.WriteLine($"{patient.Id},{uids[0]},{uids[1]},{uids[2]},{uids[3]}");
                    }
                }
                /*
                if (!File.Exists(exportFile))
                {
                    using (StreamWriter exportWriter = new Creat(exportFile))
                    {
                        exportWriter.WriteLine($"{patient.Id},{uids[0]},{uids[1]},{uids[2]},{uids[3]}");
                    }
                }*/
            }
            else { MessageBox.Show("ERROR: cannot find course with approved plan"); }
        }
        public static void WriteLog(string filePath, string logPath, string message)
        {
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            if (!File.Exists(filePath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    sw.WriteLine($"{DateTime.Now.TimeOfDay} -- {message}");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine($"{DateTime.Now.TimeOfDay} -- {message}");
                }
            }
        }
    }

}

