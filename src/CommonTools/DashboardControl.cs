using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Palaso.Reporting;
using Palaso.UI.WindowsForms.i8n;
using WeSay.Data;
using WeSay.Foundation;
using WeSay.Foundation.Dashboard;
using WeSay.LexicalModel;
using WeSay.Project;
using WeSay.UI;

namespace WeSay.CommonTools
{
    public partial class DashboardControl : UserControl, ITask, IFinishCacheSetup
    {
        private readonly IRecordListManager _recordListManager;
        private readonly ICurrentWorkTask _currentWorkTaskProvider;
        private readonly IList<TaskIndicator> _taskIndicators;
        private bool _isActive;
        private CurrentTaskIndicatorControl _currentTaskIndicator;
        public DashboardControl()
        {
            if(!DesignMode)
            {
                throw new NotSupportedException("Please use other constructor");
            }
            InitializeComponent();
        }
        public DashboardControl(IRecordListManager recordListManager, ICurrentWorkTask currentWorkTaskProvider)
        {
            if (recordListManager == null)
            {
                throw new ArgumentNullException("recordListManager");
            }
            if (currentWorkTaskProvider == null)
            {
                throw new ArgumentNullException("currentWorkTaskProvider");
            }
            _taskIndicators = new List<TaskIndicator>();
            _recordListManager = recordListManager;
            _currentWorkTaskProvider = currentWorkTaskProvider;

            //InitializeComponent();
            InitializeContextMenu();

            //having trouble with the designer, so adding this here
            LocalizationHelper helper = new LocalizationHelper(null);
            helper.Parent = this;
            helper.EndInit();
        }

        private void InitializeContextMenu() {
            ContextMenu = new ContextMenu();
            ContextMenu.MenuItems.Add("Configure this project...", OnRunConfigureTool);
            ContextMenu.MenuItems.Add("Use projector-friendly colors", OnToggleColorScheme);
            ContextMenu.MenuItems[1].Checked = DisplaySettings.Default.UsingProjectorScheme;
        }

        private void OnToggleColorScheme(object sender, EventArgs e)
        {
            DisplaySettings.Default.ToggleColorScheme();
            ContextMenu.MenuItems[1].Checked = DisplaySettings.Default.UsingProjectorScheme;
            if(_currentTaskIndicator !=null)
            {
                _currentTaskIndicator.UpdateColors();
            }
        }

        private static void OnRunConfigureTool(object sender, EventArgs e)
        {
            string dir = Directory.GetParent(Application.ExecutablePath).FullName;
            ProcessStartInfo startInfo =
                    new ProcessStartInfo(Path.Combine(dir, "WeSay Configuration Tool.exe"),
                                         string.Format("\"{0}\"", WeSayWordsProject.Project.ProjectDirectoryPath));
            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                ErrorReport.ReportNonFatalMessage("Could not start " + startInfo.FileName);
            }

            Application.Exit();
        }

        private TaskIndicator TaskIndicatorFromTask(ITask task)
        {
            TaskIndicator taskIndicator = new TaskIndicator(task);
            taskIndicator.Selected += OnTaskIndicatorSelected;
            _taskIndicators.Add(taskIndicator);
            return taskIndicator;
        }

        private void OnTaskIndicatorSelected(object sender, EventArgs e)
        {
            TaskIndicator taskIndicator = (TaskIndicator) sender;
            _currentWorkTaskProvider.ActiveTask = taskIndicator.Task;
        }

        private void AddIndicator(TaskIndicator indicator)
        {
            indicator.Dock = DockStyle.Fill;
            indicator.Margin = new Padding(70,0,20,5);
            _panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panel.RowCount = _panel.Controls.Count;
            _panel.Controls.Add(indicator);

        }

        private void InitializeProjectNameLabel()
        {
            _projectNameLabel = new Label();
            _projectNameLabel.AutoSize = true;
            _projectNameLabel.Font = new Font("Microsoft Sans Serif", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            _projectNameLabel.Location = new Point(14, 13);
            _projectNameLabel.Name = "_projectNameLabel";
            _projectNameLabel.Size = new Size(194, 31);
            _projectNameLabel.TabIndex = 0;
            _projectNameLabel.Text = BasilProject.Project.Name;
            _panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panel.Controls.Add(_projectNameLabel);
        }

        #region ITask

        public void Activate()
        {
            if (IsActive)
            {
                throw new InvalidOperationException("Activate should not be called when object is active.");
            }
            InitializeComponent();
            SuspendLayout();
            _panel.SuspendLayout();
            _panel.Controls.Clear();
            InitializeProjectNameLabel();
            IRecordList<LexEntry> entriesList = _recordListManager.GetListOfType<LexEntry>();
            ItemsToDoIndicator.MakeAllInstancesSameWidth(entriesList.Count);
            DictionaryStatusControl status = new DictionaryStatusControl(entriesList);
            _panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panel.Controls.Add(status);

            ITask currentWorkTask = _currentWorkTaskProvider.CurrentWorkTask;
            if (currentWorkTask != null)
            {
                TaskIndicator currentTaskIndicator = TaskIndicatorFromTask(currentWorkTask);
                _currentTaskIndicator = new CurrentTaskIndicatorControl(currentTaskIndicator);
                _currentTaskIndicator.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                _currentTaskIndicator.Margin = new Padding(0, 0, 15, 5);
                _panel.Controls.Add(_currentTaskIndicator);
            }

            IList<ITask> taskList = ((WeSayWordsProject) BasilProject.Project).Tasks;

#if WantToShowIndicatorsForPinnedTasks
            foreach (ITask task in taskList)
            {
                if (task != this && task.IsPinned)
                {
                    AddIndicator(TaskIndicatorFromTask(task));
                }
            }
#endif
            int count = 0;
            foreach (ITask task in taskList)
            {
                if (task != this && !task.IsPinned)// && (task != currentWorkTask))
                {
                    count++;
                }
            }

            if (count > 1 || currentWorkTask == null)
            {
#if WantToShowIndicatorsForPinnedTasks
                GroupHeader header = new GroupHeader();
                header.Name = StringCatalog.Get("~Tasks");
                AddGroupHeader(header);
#endif

                foreach (ITask task in taskList)
                {
                    if (task != this && !task.IsPinned && (task != currentWorkTask))
                    {
                        AddIndicator(TaskIndicatorFromTask(task));
                    }
                }
            }

            _isActive = true;
            _panel.ResumeLayout(false);
            ResumeLayout(true);
        }

        public void Deactivate()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Deactivate should only be called once after Activate.");
            }
            foreach (TaskIndicator taskIndicator in _taskIndicators)
            {
                taskIndicator.Selected -= OnTaskIndicatorSelected;
            }
            Controls.Clear();
            _isActive = false;
        }

        #region ITask Members

        public void GoToUrl(string url)
        {
            
        }

        #endregion

        public bool IsActive
        {
            get { return _isActive; }
        }

        public string Label
        {
            get
            {
                return
                        StringCatalog.Get("~Home",
                                          "The label for the 'dashboard'; the task which lets you see the status of other tasks and jump to them.");
            }
        }

        public Control Control
        {
            get { return this; }
        }
        const int CountNotRelevant = -1;
        /// <summary>
        /// Not relevant for this task
        /// </summary>
        public int ReferenceCount
        {
            get { return CountNotRelevant; }
        }

        public bool IsPinned
        {
            get { return true; }
        }

        public int Count
        {
            get { return CountNotRelevant; }
        }

        public int ExactCount
        {
            get { return CountNotRelevant; }
        }

        public string Description
        {
            get { return StringCatalog.Get("~Switch tasks and see current status of tasks"); }
        }

        public bool MustBeActivatedDuringPreCache
        {
            get { return false; }
        }

        #region IThingOnDashboard Members

        public WeSay.Foundation.Dashboard.DashboardGroup Group
        {
            get { return DashboardGroup.DontShow; }
        }


        public string LocalizedLabel
        {
            get { throw new NotImplementedException(); }
        }

        public ButtonStyle DashboardButtonStyle
        {
            get { throw new NotImplementedException(); }
        }

        public Image DashboardButtonImage
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        public void RegisterWithCache(ViewTemplate viewTemplate)
        {
            
        }

        #endregion

        #region IFinishCacheSetup Members

        public void FinishCacheSetup()
        {
            Activate();
            Deactivate();
        }

        #endregion


    }
}
