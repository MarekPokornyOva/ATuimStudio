using System.Collections.Specialized;

namespace System.Collections.ObjectModel
{
	public sealed class SupressableObservableCollection<T> : ObservableCollection<T>
	{
		private bool _supressNotification = false;
		public void SupressNotification()
		{
			_supressNotification = true;
		}

		public void RestoreNotification()
		{
			_supressNotification = false;
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (_supressNotification)
				return;
			base.OnCollectionChanged(e);
		}
	}
}
