using System;
using System.Collections.Generic;
using Palaso.UI.WindowsForms.i8n;
using WeSay.Data;
using WeSay.Foundation;
using WeSay.Language;
using WeSay.LexicalModel;
using WeSay.LexicalModel.Db4o_Specific;

namespace WeSay.LexicalModel
{
    public class LexEntrySortHelper: ISortHelper<string, LexEntry>
    {
        Db4oDataSource _db4oData; // for data
        WritingSystem _writingSystem;
        bool _isWritingSystemIdUsedByLexicalForm;
        
        public LexEntrySortHelper(Db4oDataSource db4oData, 
                                  WritingSystem writingSystem,
                                  bool isWritingSystemIdUsedByLexicalForm)
        {
            if(db4oData == null)
            {
                throw new ArgumentNullException("db4oData");
            }
            if (writingSystem == null)
            {
                throw new ArgumentNullException("writingSystem");
            }
            
            _db4oData = db4oData;
            _writingSystem = writingSystem;
            _isWritingSystemIdUsedByLexicalForm = isWritingSystemIdUsedByLexicalForm;
        }

        public LexEntrySortHelper(WritingSystem writingSystem,
                                  bool isWritingSystemIdUsedByLexicalForm)
        {
            if (writingSystem == null)
            {
                throw new ArgumentNullException("writingSystemId");
            }

            _writingSystem = writingSystem;
            _isWritingSystemIdUsedByLexicalForm = isWritingSystemIdUsedByLexicalForm;
        }

        #region IDb4oSortHelper<string,LexEntry> Members

        public IComparer<string> KeyComparer
        {
            get
            {
                return _writingSystem;
            }
        }


        public List<KeyValuePair<string, long>> GetKeyIdPairs()
        {
            if (_db4oData != null)
            {
                if (_isWritingSystemIdUsedByLexicalForm)
                {
                    return KeyToEntryIdInitializer.GetLexicalFormToEntryIdPairs(_db4oData,
                                                                                _writingSystem.Id);
                }
                else
                {
                    return KeyToEntryIdInitializer.GetGlossToEntryIdPairs(_db4oData,
                                                                          _writingSystem.Id);
                }
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Starting from scratch, give me all the keys for every entry
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public IEnumerable<string> GetKeys(LexEntry item)
        {
            List<string> keys = new List<string>();
            if (_isWritingSystemIdUsedByLexicalForm)
            {
                keys.Add(item.LexicalForm.GetBestAlternative(_writingSystem.Id, "*"));
            }
            else
            {
                bool hasSense = false;
                //nb: the logic here relies on gloss being a requirement of having a sense.
                //If definition were allowed instead, we'd want to go to the second clause instead of outputtig
                //empty labels.
                foreach (LexSense sense in item.Senses)
                {
                    hasSense = true;

                    keys.AddRange(KeyToEntryIdInitializer.SplitGlossAtSemicolon(sense.Gloss, _writingSystem.Id));
                }
                if(!hasSense)
                {
                    keys.Add("*" + StringCatalog.Get("~No Gloss", "This is what shows if the user is listing words by the glossing language, but the word doesn't have a gloss."));
                }
            }
            return keys;
        }


        public string Name
        {
            get
            {
                return "LexEntry sorted by " + _writingSystem.Id;
            }
        }

        public override int GetHashCode()
        {
            return _writingSystem.GetHashCode();
        }

        #endregion
    }

    public class EntryByBestLexemeFormAlternativeComparer : IComparer<LexEntry>
    {
        private readonly WritingSystem _writingSystem;

        public EntryByBestLexemeFormAlternativeComparer(WritingSystem writingSystem)
        {
            _writingSystem = writingSystem;
        }

        public int Compare(LexEntry x, LexEntry y)
        {
            string formX = x.LexicalForm.GetBestAlternative(_writingSystem.Id);
            string formY = y.LexicalForm.GetBestAlternative(_writingSystem.Id);
            return _writingSystem.Compare(formX, formY);
        }
    }
}