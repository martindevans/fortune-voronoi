using System.Collections.Generic;

namespace FortuneVoronoi
{
	public interface IPriorityQueue<T>
	{
		int Push(T o);
		T Pop();
		T Peek();
	}
	public sealed class BinaryPriorityQueue<T> : IPriorityQueue<T>
	{
	    private readonly List<T> _innerList = new List<T>();
	    private readonly IComparer<T> _comparer;

		#region contructors
		public BinaryPriorityQueue() : this(Comparer<T>.Default)
		{
        }

		public BinaryPriorityQueue(IComparer<T> c)
		{
			_comparer = c;
		}
	    #endregion

	    private void SwitchElements(int i, int j)
		{
			var h = _innerList[i];
			_innerList[i] = _innerList[j];
			_innerList[j] = h;
		}

	    private int OnCompare(int i, int j)
		{
			return _comparer.Compare(_innerList[i], _innerList[j]);
		}

		#region public methods
		/// <summary>
		/// Push an object onto the PQ
		/// </summary>
		/// <param name="o">The new object</param>
		/// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
		public int Push(T o)
		{
		    int p = _innerList.Count;
		    _innerList.Add(o); // E[p] = o
			do
			{
				if(p==0)
					break;
				var p2 = (p-1)/2;
				if(OnCompare(p,p2)<0)
				{
					SwitchElements(p,p2);
					p = p2;
				}
				else
					break;
			}while(true);
			return p;
		}

		/// <summary>
		/// Get the smallest object and remove it.
		/// </summary>
		/// <returns>The smallest object</returns>
		public T Pop()
		{
			var result = _innerList[0];
		    var p = 0;
		    _innerList[0] = _innerList[_innerList.Count-1];
			_innerList.RemoveAt(_innerList.Count-1);
			do
			{
				var pn = p;
				var p1 = 2*p+1;
				var p2 = 2*p+2;
				if(_innerList.Count>p1 && OnCompare(p,p1)>0) // links kleiner
					p = p1;
				if(_innerList.Count>p2 && OnCompare(p,p2)>0) // rechts noch kleiner
					p = p2;
				
				if(p==pn)
					break;
				SwitchElements(p,pn);
			}while(true);
			return result;
		}

		/// <summary>
		/// Get the smallest object without removing it.
		/// </summary>
		/// <returns>The smallest object</returns>
		public T Peek()
		{
			if(_innerList.Count>0)
				return _innerList[0];
			return default(T);
		}

		public bool Contains(T value)
		{
			return _innerList.Contains(value);
		}

		public void Clear()
		{
			_innerList.Clear();
		}

		public int Count
		{
			get
			{
				return _innerList.Count;
			}
		}
		#endregion
	}
}
