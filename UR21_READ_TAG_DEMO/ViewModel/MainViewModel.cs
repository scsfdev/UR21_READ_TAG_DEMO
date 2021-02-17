using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using UR21_READ_TAG_DEMO.Model;


/*
 * NOTE: Go to Project --> Projerties --> Build --> Platform target. Set it to x64. UR21 dll in this project can only work with 64 bit Windows OS.
 */ 


namespace UR21_READ_TAG_DEMO.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public ICommand CmdBrowse { get; private set; }
        public ICommand CmdRefresh { get; private set; }
        public ICommand CmdRfidAction { get; private set; }
        public ICommand CmdReset { get; private set; }
        public ICommand CmdExit { get; private set; }

        public ICommand CmdSave { get; private set; }

        Ur21 ur = new Ur21();

        DispatcherTimer dTimer;

        /// <summary>
        /// Main Entry of this file.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            Version = General.gGetVersion();

            Messenger.Default.Register<string>(this, MsgType.MAIN_VM, ShowMsg);

            DefaultMsg = "UR21 Read Demo. Created by Tin Maung Htay. Version: " + Version;

            CmdBrowse = new RelayCommand(BrowseAction);
            CmdRefresh = new RelayCommand(RefreshAction);
            CmdRfidAction = new RelayCommand<object>(RfidAction);
            CmdReset = new RelayCommand(ResetForm);
            CmdExit = new RelayCommand(ExitForm);
            CmdSave = new RelayCommand(SaveTagData);
            Ur21.OnTagRead += Ur_OnTagRead;

            dTimer = new DispatcherTimer();
            dTimer.Interval = TimeSpan.FromSeconds(2);
            dTimer.Tick += DTimer_Tick;

            ResetForm();
        }

        


        #region Getter / Setter

        private string defaultMsg;

        public string DefaultMsg
        {
            get { return defaultMsg; }
            set { Set(ref defaultMsg, value); }
        }



        private string version;
        public string Version
        {
            get { return version; }
            set { Set(ref version, value); }
        }



        private string statusMsg;
        public string StatusMsg
        {
            get { return statusMsg; }
            set { Set(ref statusMsg, value); }
        }



        private bool enableSave;
        public bool EnableSave
        {
            get { return enableSave; }
            set { Set(ref enableSave, value); }
        }



        private bool inAction;
        public bool InAction
        {
            get { return inAction; }
            set { Set(ref inAction, value); }
        }


        private bool isReady;
        public bool IsReady
        {
            get { return isReady; }
            set { Set(ref isReady, value); }
        }


        private string saveLocation;
        public string SaveLocation
        {
            get { return saveLocation; }
            set { Set(ref saveLocation, value); }
        }



        private string comPort;
        public string ComPort
        {
            get { return comPort; }
            set { Set(ref comPort, value); }
        }



        private bool allowDuplicate;
        public bool AllowDuplicate
        {
            get { return allowDuplicate; }
            set { Set(ref allowDuplicate, value); }
        }



        private string rfCmdText;
        public string RfCmdText
        {
            get { return rfCmdText; }
            set { Set(ref rfCmdText, value); }
        }



        private string selected_Antenna;
        public string Selected_Antenna
        {
            get { return selected_Antenna; }
            set { Set(ref selected_Antenna, value); }
        }



        private ObservableCollection<Tag> tagList;
        public ObservableCollection<Tag> TagList
        {
            get { return tagList; }
            set { Set(ref tagList, value); }
        }

        #endregion



        #region Custom Functions

        private void ShowMsg(string strMsg)
        {
            StatusMsg = strMsg.Replace(MyConst.ERROR, "Error:").Replace(Environment.NewLine, " ").Replace("Error Details", "Details");
            Messenger.Default.Send(strMsg, MsgType.MAIN_V);
            RfidAction(MyConst.STOP);
        }


        private void ExitForm()
        {
            dTimer.Stop();
            Messenger.Default.Send(new NotificationMessage(this, MyConst.EXIT));
        }


        private void RefreshAction()
        {
            Auto_Get_COM_Ports();
        }


        private void ResetForm()
        {
            StatusMsg = defaultMsg;

            AllowDuplicate = false;
            SaveLocation = null;
            
            Selected_Antenna = "";
            RfCmdText = MyConst.START;

            InAction = false;
            IsReady = true;
            EnableSave = false;

            TagList = new ObservableCollection<Tag>();

            dTimer.Start();
        }


        private void Auto_Get_COM_Ports(bool bSilent = false)
        {
            try
            {
                string strPORT = "";
                int iCount = 0;

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_SerialPort"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if (queryObj["Caption"] != null && queryObj["Caption"].ToString().Trim().ToUpper().Contains("DENSO WAVE"))
                        {
                            if (!queryObj["Caption"].ToString().Trim().ToUpper().Contains("DISCONNECTED") && queryObj["DeviceID"] != null)
                            {
                                strPORT = queryObj["DeviceID"].ToString().ToUpper().Replace("COM", "");
                                iCount += 1;
                            }
                        }
                    }
                }

                if (iCount > 1)
                {
                    StatusMsg = "Warning: More than one DENSO WAVE USB-COM devices are detected. Disconnect the one you don't need.";

                    if (!bSilent)
                        Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + "More than one DENSO WAVE USB-COM devices are detected. Disconnect the one you don't need",
                                            MsgType.MAIN_V);
                }
                else
                {
                    if (string.IsNullOrEmpty(strPORT))
                    {
                        StatusMsg = "Warning: No DENSO WAVE USB-COM device is connected to this PC!";

                        if (!bSilent)
                            Messenger.Default.Send(MyConst.WARNING + Environment.NewLine + "No DENSO WAVE USB-COM device is connected to this PC!", MsgType.MAIN_V);
                    }
                    else
                    {
                        ComPort = strPORT;      // Assign to variable.
                        dTimer.Stop();
                        IsActionReady();
                        StatusMsg = DefaultMsg;
                    }
                }
            }
            catch (ManagementException e)
            {
                dTimer.Stop();
                StatusMsg = "Error: Process failed while trying to retrieve COM port. Details: " + e.Message;
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + "An error occurred while trying to retrieve COM port." + Environment.NewLine +
                                        Environment.NewLine + "Error detail: " + e.Message, MsgType.MAIN_V);
            }
        }


        private void BrowseAction()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save read tag data to...";
            sfd.FileName = "";
            sfd.Filter = "*.csv|*.csv";
            sfd.InitialDirectory = string.IsNullOrEmpty(Properties.Settings.Default.SAVE_PATH) ? "C:\\" : Properties.Settings.Default.SAVE_PATH.Trim();
            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                SaveLocation = sfd.FileName;

                IsActionReady();
            }
        }


        private void IsActionReady()
        {
            //if (!string.IsNullOrEmpty(saveLocation) && !string.IsNullOrEmpty(comPort))
            //    InAction = true;
            //else
            //    InAction = false;

            InAction = true;
        }



        private void DTimer_Tick(object sender, EventArgs e)
        {
            Auto_Get_COM_Ports(true);
        }


        private void RfidAction(object objAction)
        {
            // Update save file location for next references.
            if (saveLocation != null)
            {
                Properties.Settings.Default.SAVE_PATH = Path.GetDirectoryName(saveLocation);
                Properties.Settings.Default.Save();
            }
            //else
            //    return;

            if (objAction == null)
                return;

            try
            {
                // Get COM port number in byte.
                byte bPort = byte.Parse(ComPort);

                if (objAction.ToString() == MyConst.START)
                {
                    if(dTimer.IsEnabled)
                        dTimer.Stop();

                    // Start RFID reading.
                    RfCmdText = MyConst.STOP;
                    TagList = new ObservableCollection<Tag>();
                    InAction = true;
                    IsReady = false;
                    EnableSave = false;
                    ur.StartRead(bPort);
                }
                else if (objAction.ToString() == MyConst.STOP)
                {
                    // Stop RFID reading.
                    RfCmdText = MyConst.START;
                    ur.StopReading();

                    IsReady = true;
                    InAction = false;
                    EnableSave = true;
                    dTimer.Start();
                }
            }
            catch (Exception e)
            {
                StatusMsg = "Error: Process failed while reading tag. Details: " + e.Message;
                Messenger.Default.Send(MyConst.ERROR + Environment.NewLine + e.Message, MsgType.MAIN_V);
            }
        }


        private List<Tag> CloneTag(List<Tag> lstSource)
        {
            List<Tag> lstReturn = new List<Tag>(lstSource.Count);

            lstSource.ForEach((item) =>
            {
                lstReturn.Add(new Tag(item));
            });

            return lstReturn;
        }


        private void Ur_OnTagRead(object sender, TagArgs e)
        {
            if (e != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(delegate
                {
                    int iCount = 0;
                    int iQty = 1;

                    if (TagList == null)
                        TagList = new ObservableCollection<Tag>();
                    else
                        iCount = TagList.Count;

                    bool bExist = false;

                    if (TagList.Count > 0)
                    {
                        List<Tag> lst = TagList.ToList();

                        foreach (Tag ta in lst)
                        {
                            if (ta.Uii == e.Uii)
                            {
                                bExist = true;

                                if (allowDuplicate)
                                {
                                    // Need to increase Qty.
                                    ta.Qty += 1;
                                    ta.ReadDate = DateTime.Now.ToString("yyyy-MM-dd");
                                    ta.ReadTime = DateTime.Now.ToString("hh:mm:ss tt");
                                }
                            }
                        }

                        if (bExist)
                        {
                            TagList.Clear();
                            TagList = new ObservableCollection<Tag>(lst);
                        }
                    }

                    if (!bExist)
                    {
                        iCount++;

                        Tag t = new Tag();
                        t.Uii = e.Uii;
                        t.No = iCount;
                        t.Qty = iQty;
                        t.ReadDate = DateTime.Now.ToString("yyyy-MM-dd");
                        t.ReadTime = DateTime.Now.ToString("hh:mm:ss tt");

                        TagList.Add(t);
                    }
                });
            }
        }



        private void SaveTagData()
        {
            if (tagList == null || tagList.Count <= 0)
                return;

            if (General.gSaveData(SaveLocation, tagList.ToList()))
                ResetForm();
        }


        #endregion

    }
}