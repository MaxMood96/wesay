using System;
using System.Windows.Forms;
using NUnit.Framework;
using NUnit.Extensions.Forms;
using WeSay.Data;
using WeSay.LexicalModel;
using WeSay.LexicalModel.Db4o_Specific;
using WeSay.Project;
using WeSay.UI;

namespace WeSay.LexicalTools.Tests
{
	[TestFixture]
	public class EntryDetailControlTests : NUnit.Extensions.Forms.NUnitFormTest
	{
		private EntryDetailTask _task;
		IRecordListManager _recordListManager;
		string _filePath;
		private IRecordList<LexEntry> _records;
		private string _vernacularWsId;
		private TabControl _tabControl;
		private Form _window;
		private TabPage _detailTaskPage;

		public override void Setup()
		{
			base.Setup();
//            Db4oLexModelHelper.InitializeForNonDbTests();
			BasilProject.InitializeForTests();
			this._vernacularWsId = BasilProject.Project.WritingSystems.VernacularWritingSystemDefault.Id;

			this._filePath = System.IO.Path.GetTempFileName();
			Db4oModelConfiguration.Configure();
			this._recordListManager = new Db4oRecordListManager(_filePath);
			Db4oLexModelHelper.Initialize(((Db4oRecordListManager)_recordListManager).DataSource.Data);

			this._records = this._recordListManager.GetListOfType<LexEntry>();
			AddEntry("Initial");
			AddEntry("Secondary");
			AddEntry("Tertiary");

			string[] analysisWritingSystemIds = new string[] { BasilProject.Project.WritingSystems.AnalysisWritingSystemDefaultId };
			string[] vernacularWritingSystemIds = new string[] { BasilProject.Project.WritingSystems.VernacularWritingSystemDefaultId };
			ViewTemplate viewTemplate = new ViewTemplate();
			viewTemplate.Add(new Field(Field.FieldNames.EntryLexicalForm.ToString(), vernacularWritingSystemIds));
			viewTemplate.Add(new Field(Field.FieldNames.SenseGloss.ToString(), analysisWritingSystemIds));
			viewTemplate.Add(new Field(Field.FieldNames.ExampleSentence.ToString(), vernacularWritingSystemIds));
			viewTemplate.Add(new Field(Field.FieldNames.ExampleTranslation.ToString(), analysisWritingSystemIds));


			this._task = new EntryDetailTask(_recordListManager, viewTemplate);
			this._detailTaskPage = new TabPage();
			ActivateTask();

			this._tabControl = new TabControl();

			this._tabControl.Dock = DockStyle.Fill;
			this._tabControl.TabPages.Add(this._detailTaskPage);
			this._tabControl.TabPages.Add("Dummy");
			this._window = new Form();
			this._window.Controls.Add(this._tabControl);
			this._window.Show();
		}


		/// <summary>
		/// DOESN'T WORK (this version of nunit forms seems broken in this respect)
		/// </summary>
		public override bool UseHidden
		{
			get
			{
				return true;
			}
		}

		private void AddEntry(string lexemeForm)
		{
			LexEntry entry = new LexEntry();
			entry.LexicalForm.SetAlternative(this._vernacularWsId, lexemeForm);
			this._records.Add(entry);
		}

		public override void TearDown()
		{
			_window.Close();
			_window.Dispose();
			_window = null;
			this._recordListManager.Dispose();
			_recordListManager = null;
			_records.Dispose();
			_records = null;
			System.IO.File.Delete(_filePath);
			base.TearDown();
		}

		[Test]
		public void ClickingAddWordIncreasesRecordsByOne()
		{
			int before = _records.Count;
			ClickAddWord();
			Assert.AreEqual(1 + before, _records.Count);
		}

		[Test]
		public void ClickingAddWordShowsEmptyRecord()
		{
			LexicalFormMustMatch("Initial");
			ClickAddWord();
			LexicalFormMustMatch("");
		}

		[Test]
		public void ClickingAddWordSelectsNewWordInList()
		{
			Assert.AreEqual("Initial", LexemeFormOfSelectedEntry);
			ClickAddWord();
			Assert.AreEqual("", LexemeFormOfSelectedEntry);
		}

		[Test]
		public void ClickingDeleteWordDereasesRecordsByOne()
		{
			int before = _records.Count;
			ClickDeleteWord();
			Assert.AreEqual(before-1, _records.Count);
		}



		[Test]
		public void DeletingFirstWordSelectsNextWordInList()
		{
			Assert.AreEqual("Initial", LexemeFormOfSelectedEntry);
			ClickDeleteWord();
			Assert.AreEqual("Secondary", LexemeFormOfSelectedEntry);
		}

		[Test]
		public void DeletingLastWordSelectsPreviousWordInList()
		{
			BindingListGridTester t = new BindingListGridTester("_recordsListBox");
			t.Properties.SelectedIndex = 2;
			Assert.AreEqual("Tertiary", LexemeFormOfSelectedEntry);
			ClickDeleteWord();
			Assert.AreEqual("Secondary", LexemeFormOfSelectedEntry);
		}

		[Test]
		public void AddWordsThenDeleteDoesNotCrash()
		{
			ClickAddWord();
			ClickDeleteWord();
		}

		[Test]
		public void DeletingAllWordsThenAddingDoesNotCrash()
		{
			ClickDeleteWord();
			ClickDeleteWord();
			ClickAddWord();
		}

		[Test]
		public void IfNoWordsDeleteButtonDisabled()
		{
			NUnit.Extensions.Forms.LinkLabelTester l = new LinkLabelTester("_btnDeleteWord");
			Assert.IsTrue(l.Properties.Enabled);
			ClickDeleteWord();
			ClickDeleteWord();
			ClickDeleteWord();
			Assert.IsFalse(l.Properties.Enabled);
		}

		[Test]
		public void SwitchingToAnotherTaskDoesNotLooseBindings()
		{
		   LexicalFormMustMatch("Initial");
			TypeInLexicalForm("one");
			this._task.Deactivate();
			_tabControl.SelectedIndex = 1;
			_tabControl.SelectedIndex = 0;
			ActivateTask();

			NUnit.Extensions.Forms.TextBoxTester t = new TextBoxTester(GetLexicalFormControlName());
			t.Properties.Visible = true;

			LexicalFormMustMatch("one");

			TypeInLexicalForm("plus"); // need something that still sorts higher than Secondary and Tertiary
			this._task.Deactivate();
			_tabControl.SelectedIndex = 1;
			_tabControl.SelectedIndex = 0;
			ActivateTask();
			t = new TextBoxTester(GetLexicalFormControlName());
			t.Properties.Visible = true;
			LexicalFormMustMatch("plus");
		}

		private void ActivateTask()
		{
			this._task.Activate();
			this._task.Control.Dock = DockStyle.Fill;
			this._detailTaskPage.Controls.Clear();
			this._detailTaskPage.Controls.Add(this._task.Control);
		}

		private static void LexicalFormMustMatch(string value)
		{
			NUnit.Extensions.Forms.TextBoxTester t = new TextBoxTester(GetLexicalFormControlName());
			Assert.AreEqual(value, t.Properties.Text);
		}

		private static string GetLexicalFormControlName()
		{
			return Field.FieldNames.EntryLexicalForm.ToString() +"_" + BasilProject.Project.WritingSystems.VernacularWritingSystemDefaultId;
		}

		private static void TypeInLexicalForm(string value)
		{
			NUnit.Extensions.Forms.TextBoxTester t = new TextBoxTester(GetLexicalFormControlName());
			t.Properties.Text = value;
		}

		private string LexemeFormOfSelectedEntry
		{
			get
			{
				BindingListGridTester t = new BindingListGridTester("_recordsListBox");
				return ((LexEntry)t.Properties.SelectedObject).LexicalForm.GetAlternative(_vernacularWsId);
			}
		}

		private static void ClickAddWord()
		{
			NUnit.Extensions.Forms.LinkLabelTester l = new LinkLabelTester("_btnNewWord");
			l.Click();
		}

		private void ClickDeleteWord()
		{
			NUnit.Extensions.Forms.LinkLabelTester l = new LinkLabelTester("_btnDeleteWord");
			l.Click();
		}

		[Test]
		public void FindTextChanged_ButtonSaysFind()
		{
			TextBoxTester t = new TextBoxTester("_findText");
			t.Enter("changed");
			ButtonTester b = new ButtonTester("_btnFind");
			Assert.AreEqual("Find", b.Text);
		}


		[Test]
		public void EnterText_PressFindButton_Finds()
		{
			TextBoxTester t = new TextBoxTester("_findText");
			t.Enter("Secondary");
			ButtonTester b = new ButtonTester("_btnFind");
			b.Click();
			BindingListGridTester l = new BindingListGridTester("_recordsListBox");

			Assert.AreEqual("Secondary", ((LexEntry)l.Properties.SelectedObject).ToString());
			RichTextBoxTester r = new RichTextBoxTester("_lexicalEntryPreview");
			Assert.IsTrue(r.Text.Contains("Secondary"));
		}

		[Test]
		public void FindText_Enter_Finds()
		{
			TextBoxTester t = new TextBoxTester("_findText");
			t.Enter("Secondary");
			t.FireEvent("KeyDown", new KeyEventArgs(Keys.Enter));
			BindingListGridTester l = new BindingListGridTester("_recordsListBox");

			Assert.AreEqual("Secondary", ((LexEntry)l.Properties.SelectedObject).ToString());
		}
	}
}
