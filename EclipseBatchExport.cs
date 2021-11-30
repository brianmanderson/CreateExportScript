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
[assembly: AssemblyVersion("1.0.0.3")]
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
            string logPath = @"\\ucsdhc-varis2\radonc$\BMAnderson\exportDicomTest";
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
                if (str.Length == 2)
                {
                    string planName = str[1];
                    WriteTxtFile(patient, planName, logPath);
                }
                else
                {
                    WriteTxtFileAllApprovedPlans(patient, logPath);
                }
                app.ClosePatient();
            }
        }
        public static void WriteTxtFileAllApprovedPlans(Patient patient, string logPath)
        {
            List<Course> courses = new List<Course>();

            List<PlanSetup> tmpplanSetups = new List<PlanSetup>();
            List<string> planNames = new List<string>();
            List<PlanSetup> planSetups = new List<PlanSetup>();
            List<Course> targetCourses = new List<Course>();
            Course targetCourse;
            PlanSetup planSetup;
            courses = patient.Courses.ToList();
            foreach (Course c in courses)
            {
                tmpplanSetups = c.PlanSetups.ToList();
                foreach (PlanSetup p in tmpplanSetups)
                {
                    //MessageBox.Show(p.ApprovalStatus.ToString());
                    if (p.ApprovalStatus.ToString() == "TreatmentApproved")
                    {
                        targetCourses.Add(c);
                        planNames.Add(p.Id);
                    }
                }

            }
            for (int i = 0; i < targetCourses.Count; i++)
            {
                targetCourse = targetCourses[i];
                planSetups = targetCourse.PlanSetups.ToList();
                planSetup = planSetups.FirstOrDefault(x => x.Id == planNames[i]);
                string expInstructions = string.Format("ExportInstructions.txt");
                string exportFile = System.IO.Path.Combine(logPath, expInstructions);

                if (planSetup.StructureSet is null)
                {
                    continue; // Could be an electron plan with no structure set or imaging, in which case, move on
                }
                //finding UID
                string[] uids = { planSetup.UID.ToString(), planSetup.StructureSet.UID, planSetup.Dose.UID, planSetup.StructureSet.Image.Series.UID };


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
            }
        }

        public static void WriteTxtFile(Patient patient, string planName,string logPath)
        {
            List<Course> courses = new List<Course>();
            
            List<PlanSetup> tmpplanSetups = new List<PlanSetup>();
            List<PlanSetup> planSetups = new List<PlanSetup>();

            Course targetCourse = null;
            PlanSetup planSetup;
            courses = patient.Courses.ToList();
            foreach(Course c in courses)
            {
                tmpplanSetups = c.PlanSetups.ToList();
                foreach(PlanSetup p in tmpplanSetups)
                {
                    //MessageBox.Show(p.ApprovalStatus.ToString());
                    if (p.Id == planName && p.ApprovalStatus.ToString() == "TreatmentApproved")
                    {
                        targetCourse = c;
                    }
                }

            }
            if (targetCourse != null)
            {
                planSetups = targetCourse.PlanSetups.ToList();
                string expInstructions = string.Format("ExportInstructions.txt");
                string exportFile = System.IO.Path.Combine(logPath, expInstructions);
                planSetup = planSetups.FirstOrDefault(x => x.Id == planName);

                //finding UID
                string[] uids = { planSetup.UID.ToString(), planSetup.StructureSet.UID, planSetup.Dose.UID, planSetup.StructureSet.Image.Series.UID };


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

