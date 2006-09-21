using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;

namespace WeSay.Data
{
	public class InMemoryRecordList<T> : IRecordList<T> where T : class, new()
	{
		List<T> _list;
		PropertyDescriptor _sortProperty;
		ListSortDirection _listSortDirection;
		bool _isSorted;
		bool _isFiltered;
		PropertyDescriptorCollection _pdc;


		public InMemoryRecordList()
		{
			_list = new List<T>();
			_pdc =  TypeDescriptor.GetProperties(typeof(T));
		}

		public InMemoryRecordList(IRecordList<T> original)
			: this()
		{
			this.AddRange(original);

			_isSorted = original.IsSorted;
			_sortProperty = original.SortProperty;
			_listSortDirection = original.SortDirection;

			_isFiltered = original.IsFiltered;
		}

		bool IRecordList<T>.Commit()
		{
			return true;
		}

		public void RegisterItemPropertyChangedHandler(T item, bool register)
		{
			VerifyNotDisposed();
			INotifyPropertyChanged localItem = item as INotifyPropertyChanged;
			if (localItem == null)
			{
				return;
			}
			localItem.PropertyChanged -= Item_PropertyChanged;
			if (register)
			{
				localItem.PropertyChanged += Item_PropertyChanged;
			}
		}

		private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnItemChanged(_list.IndexOf((T)sender), e.PropertyName);
		}

		public void AddRange(IEnumerable<T> collection)
		{
			VerifyNotDisposed();
			IEnumerator<T> enumerator = collection.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Add(enumerator.Current);
			}
		}

		public void AddRange(IEnumerable collection)
		{
			VerifyNotDisposed();
			IEnumerator enumerator = collection.GetEnumerator();
			while (enumerator.MoveNext())
			{
				Add((T)enumerator.Current);
			}
		}

		#region IBindingList Members

		void IBindingList.AddIndex(PropertyDescriptor property)
		{
			VerifyNotDisposed();
		}

		public T AddNew()
		{
			VerifyNotDisposed();
			T o = new T();
			Add(o);
			return o;
		}

		object IBindingList.AddNew()
		{
			VerifyNotDisposed();
			return AddNew();
		}

		bool IBindingList.AllowEdit
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}
		bool IBindingList.AllowNew
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}
		bool IBindingList.AllowRemove
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}

		public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
		{
			VerifyNotDisposed();
			if (_list.Count > 1)
			{
				Comparison<T> sort = delegate(T item1, T item2)
				{
					PropertyComparison<T> propertySorter = ComparisonHelper<T>.GetPropertyComparison(ComparisonHelper<T>.DefaultPropertyComparison, direction);
					return propertySorter(item1, item2, property);
				};

				_list.Sort(sort);
				_sortProperty = property;
				_listSortDirection = direction;
				_isSorted = true;
				OnListReset();
			}
		}

		int IBindingList.Find(PropertyDescriptor property, object key)
		{
			VerifyNotDisposed();
			throw new NotSupportedException();
		}

		public bool IsSorted
		{
			get
			{
				VerifyNotDisposed();
				return _isSorted;
			}
		}

		protected virtual void OnItemAdded(int newIndex)
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, newIndex));
		}

		protected virtual void OnItemChanged(int newIndex)
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, newIndex));
		}

		protected virtual void OnItemChanged(int newIndex, string field)
		{
			PropertyDescriptor propertyDescriptor = _pdc.Find(field, false);
			if (propertyDescriptor == null)
			{
				OnItemChanged(newIndex);
			}

			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, newIndex, propertyDescriptor));
		}

		protected virtual void OnItemDeleted(int oldIndex)
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, oldIndex));
		}

		protected virtual void OnListReset()
		{
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
		}

		protected virtual void OnListChanged(ListChangedEventArgs e)
		{
			this.ListChanged(this, e);
		}

		public event ListChangedEventHandler ListChanged = delegate
		{
		};

		void IBindingList.RemoveIndex(PropertyDescriptor property)
		{
			VerifyNotDisposed();
		}

		public void RemoveSort()
		{
			VerifyNotDisposed();
			if (IsSorted)
			{
				_isSorted = false;
				_sortProperty = null;
				OnListReset();
			}
		}

		public ListSortDirection SortDirection
		{
			get
			{
				VerifyNotDisposed();
				return _listSortDirection;
			}
		}

		public PropertyDescriptor SortProperty
		{
			get
			{
				VerifyNotDisposed();
				return _sortProperty;
			}
		}

		bool IBindingList.SupportsChangeNotification
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}

		bool IBindingList.SupportsSearching
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		bool IBindingList.SupportsSorting
		{
			get
			{
				VerifyNotDisposed();
				return true;
			}
		}

		#endregion

		#region IFilterable<T> Members

		public void ApplyFilter(Predicate<T> itemsToInclude)
		{
			VerifyNotDisposed();
			if (itemsToInclude == null)
			{
				throw new ArgumentNullException();
			}
			Predicate<T> itemsToExclude = ComparisonHelper<T>.GetInversePredicate(itemsToInclude);
			_list.RemoveAll(itemsToExclude);
			_isFiltered = true;
			OnListReset();
		}

		public void RemoveFilter()
		{
			VerifyNotDisposed();
			throw new NotImplementedException();
			//_records.Filter = null;
			//OnListReset();
		}

		public void RefreshFilter()
		{
			VerifyNotDisposed();
			throw new NotImplementedException();
			//OnListReset();
		}

		public bool IsFiltered
		{
			get
			{
				VerifyNotDisposed();
				return _isFiltered;
			}
		}
#endregion
		#region IList<T> Members

		public int IndexOf(T item)
		{
			VerifyNotDisposed();
			return _list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			VerifyNotDisposed();
			_list.Insert(index, item);
			OnItemChanged(index);
		}

		public void RemoveAt(int index)
		{
			VerifyNotDisposed();
			RegisterItemPropertyChangedHandler(this[index], false);
			_list.RemoveAt(index);
			OnItemDeleted(index);
		}

		public T this[int index]
		{
			get
			{
				VerifyNotDisposed();
				return _list[index];
			}
			set
			{
				VerifyNotDisposed();
				RegisterItemPropertyChangedHandler(this[index], false);
				_list[index] = value;
				RegisterItemPropertyChangedHandler(value, true);

				OnItemChanged(index);
			}
		}

		#endregion

		#region IList Members

		int System.Collections.IList.Add(object value)
		{
			VerifyNotDisposed();
			T item = (T)value;
			Add(item);
			return IndexOf(item);
		}

		void System.Collections.IList.Clear()
		{
			VerifyNotDisposed();
			Clear();
		}

		bool System.Collections.IList.Contains(object value)
		{
			VerifyNotDisposed();
			return Contains((T)value);
		}

		int System.Collections.IList.IndexOf(object value)
		{
			VerifyNotDisposed();
			return IndexOf((T)value);
		}

		void System.Collections.IList.Insert(int index, object value)
		{
			VerifyNotDisposed();
			Insert(index, (T)value);
		}

		public bool IsFixedSize
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		bool System.Collections.IList.IsReadOnly
		{
			get
			{
				VerifyNotDisposed();
				return IsReadOnly;
			}
		}

		void System.Collections.IList.Remove(object value)
		{
			VerifyNotDisposed();
			Remove((T)value);
		}

		private void CheckIndex(int index)
		{
			if (index < 0 || index >= Count)
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		void System.Collections.IList.RemoveAt(int index)
		{
			VerifyNotDisposed();
			CheckIndex(index);
			RemoveAt(index);
		}

		object System.Collections.IList.this[int index]
		{
			get
			{
				VerifyNotDisposed();
				CheckIndex(index);
				return _list[index];
			}
			set
			{
				VerifyNotDisposed();
				CheckIndex(index);
				_list[index] = (T)value;
				OnItemChanged(index);
			}
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			VerifyNotDisposed();
			_list.Add(item);
			RegisterItemPropertyChangedHandler(item, true);
			OnItemAdded(IndexOf(item));
		}

		public void Clear()
		{
			VerifyNotDisposed();
			int count = _list.Count;
			foreach (T item in _list)
			{
				RegisterItemPropertyChangedHandler(item, false);
			}

			_list.Clear();
			OnListReset();
		}

		public bool Contains(T item)
		{
			VerifyNotDisposed();
			return _list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			VerifyNotDisposed();
			_list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				VerifyNotDisposed();
				return _list.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		public bool Remove(T item)
		{
			VerifyNotDisposed();
			return _list.Remove(item);
		}

		#endregion

		#region ICollection Members

		void System.Collections.ICollection.CopyTo(Array array, int index)
		{
			VerifyNotDisposed();
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, "must be >= 0");
			}
			if (index + Count > array.Length)
			{
				throw new ArgumentException("array not large enough to fit collection starting at index");
			}
			if (array.Rank > 1)
			{
				throw new ArgumentException("array cannot be multidimensional", "array");
			}

			T[] tArray = new T[Count];
			CopyTo(tArray, 0);
			foreach (T t in tArray)
			{
				array.SetValue(t, index++);
			}
		}

		int System.Collections.ICollection.Count
		{
			get
			{
				VerifyNotDisposed();
				return Count;
			}
		}

		bool System.Collections.ICollection.IsSynchronized
		{
			get
			{
				VerifyNotDisposed();
				return false;
			}
		}

		object System.Collections.ICollection.SyncRoot
		{
			get
			{
				VerifyNotDisposed();
				return this;
			}
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			VerifyNotDisposed();
			return ((System.Collections.IEnumerable)_list).GetEnumerator();
		}

		#endregion

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			VerifyNotDisposed();
			return ((IEnumerable<T>)_list).GetEnumerator();
		}

		#endregion


		#region IEquatable<IRecordList<T>> Members

		public bool Equals(IRecordList<T> other)
		{
			VerifyNotDisposed();
			if (other == null)
			{
				return false;
			}
			if (this.Count != other.Count)
			{
				return false;
			}
			for (int i = 0; i < this.Count; i++)
			{
				// must be in same order to be equal
				if (this[i] != other[i])
				{
					return false;
				}
			}
			return true;
		}

		public override bool Equals(object obj)
		{
			VerifyNotDisposed();
			if (obj == null)
			{
				return false;
			}
			IRecordList<T> recordList = obj as IRecordList<T>;
			if (recordList == null)
			{
				return false;
			}

			return Equals(recordList);
		}
		#endregion

		public override int GetHashCode()
		{
			VerifyNotDisposed();
			int hashCode = _list.GetHashCode();

			if (_isSorted)
			{
				hashCode ^= _sortProperty.GetHashCode() ^ _listSortDirection.GetHashCode();

			}

			return hashCode;
		}

		#region IDisposable Members
#if DEBUG
		~InMemoryRecordList()
		{
			if (!this._disposed)
			{
				throw new ApplicationException("Disposed not explicitly called on InMemoryRecordList.");
			}
		}
#endif

		private bool _disposed = false;

		public bool IsDisposed
		{
			get
			{
				return _disposed;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.IsDisposed)
			{
				if (disposing)
				{
					// dispose-only, i.e. non-finalizable logic
					foreach (T item in _list)
					{
						RegisterItemPropertyChangedHandler(item, false);
					}
					_list = null;
				}

				// shared (dispose and finalizable) cleanup logic
				this._disposed = true;
			}
		}

		protected void VerifyNotDisposed()
		{
			if (this._disposed)
			{
				throw new ObjectDisposedException("InMemoryRecordList");
			}
		}
		#endregion

	}

}
