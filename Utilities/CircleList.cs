using System.Collections.Generic;

namespace Utility.Container
{
    public class CircleList<T>
    {
        int mCapacity;

        // mNIndex: Growed Reverse Clockwise direction(negtive order in array index, defined as Reverse Clockwise direction)
        int mNIndex;
        int mNFreeIndex;

        // mMIndex: Growed positive direction(positive order in array index, defined as Clockwise direction)
        int mMIndex;
        int mMFreeIndex;

        int mYinYang;

        T[] mItemList = null;

        public CircleList<T> Clone(int newSize = 0)
        {
            if (newSize < Count)
                newSize = Count;
            CircleList<T> result = new CircleList<T>(newSize);
            foreach (var t1 in this)
                result.Push(t1);

            result.IsAutoAdaptive = IsAutoAdaptive;
            return result;
        }

        public void Reset()
        {
            for (int i = 0; i < mCapacity; i++)
                mItemList[i] = default(T);
            mYinYang = -1;
            mNIndex = mNFreeIndex = 0;
            mMIndex = mMFreeIndex = 0;
        }

        public CircleList(int capacity, bool isAutoAdaptive)
        {
            Init(capacity);
            IsAutoAdaptive = isAutoAdaptive;
        }

        public CircleList(int capacity)
        {
            Init(capacity);
        }

        public CircleList()
        {
            Init(20);
        }

        protected void Init(int capacity)
        {
            mItemList = new T[capacity];
            mCapacity = capacity;
            mYinYang = -1;

            mNIndex = mNFreeIndex = 0;
            mMIndex = mMFreeIndex = 0;

            IsAutoAdaptive = false;
        }

        protected void moveClockwise(ref int index, int step)
        {
            index = (index + step) % mCapacity;
        }

        protected void moveReverseClockwise(ref int index, int step)
        {
            index += mCapacity - step; //(step % mCapacity);
            index %= mCapacity;
        }

        protected void reAdaptCapacity(int newCapaity)
        {
            int dataCount = Count;
            var oldItemList = mItemList;

            if (newCapaity < dataCount)
                newCapaity = dataCount;

            mItemList = new T[newCapaity];

            // copy all data
            if (dataCount > 0)
            {
                int count = 0;
                mItemList[count++] = oldItemList[mNIndex];
                int i = mNIndex;
                moveClockwise(ref i, 1);
                for (; i != mMIndex; moveClockwise(ref i, 1))
                    mItemList[count++] = oldItemList[i];
            }

            // reset meta data
            mNIndex = 0;
            mNFreeIndex = 0;
            mMIndex = dataCount;
            mMFreeIndex = mMIndex;

            mCapacity = newCapaity;
            if (dataCount == 0)
                mYinYang = -1;
            else if (dataCount == mCapacity)
                mYinYang = 1;
            else
                mYinYang = 0;
        }

        public int Capacity { get { return mCapacity; } }

        public bool IsAutoAdaptive {get; set;}

        public int Count
        {
            get
            {
                if (mYinYang < 0)
                    return 0;

                if (mYinYang > 0)
                    return mCapacity;

                return (mCapacity - mNIndex + mMIndex) % mCapacity;
            }
        }

        public int FreeCount
        {
            get
            {
                return mCapacity - Count;
            }
        }

        public int NFree
        {
            get
            {
                if (mNFreeIndex <= mNIndex)
                    return mNIndex - mNFreeIndex;
                else
                    return mCapacity - mNFreeIndex + mNIndex;
            }
        }

        public int MFree
        {
            get
            {
                if (mMIndex <= mMFreeIndex)
                    return mMFreeIndex - mMIndex;
                else
                    return mCapacity - mMIndex + mMFreeIndex;
            }
        }

        protected void tryMoreCapacity()
        {
            if (!IsAutoAdaptive)
                return;

            var newCapaity = mCapacity > 512 ? mCapacity + (1024 - mCapacity % 1024) : mCapacity + mCapacity;
            if (newCapaity < mCapacity)
                return;

            reAdaptCapacity(newCapaity);
        }

        protected int allocForN(int allocNum)
        {
            if (allocNum <= 0)
                return 0;
            if (FreeCount < allocNum)
            {
                tryMoreCapacity();
                if (FreeCount < allocNum)
                    return 0;
            }

            moveReverseClockwise(ref mNFreeIndex, allocNum);

            return allocNum;
        }

        protected int freeFromN(int freeNum)
        {
            if (freeNum <= 0 || Count <= 0)
                return 0;

            if (freeNum >= Count)
            {
                freeNum = Count;
                mYinYang = -1;
            }
            else
            {
                mYinYang = 0;
            }

            moveClockwise(ref mNIndex, freeNum);

            return freeNum;
        }

        protected int allocForM(int allocNum)
        {
            if (allocNum <= 0)
                return 0;
            if (FreeCount < allocNum)
            {
                tryMoreCapacity();
                if (FreeCount < allocNum)
                    return 0;
            }

            moveClockwise(ref mMFreeIndex, allocNum);

            return allocNum;
        }

        protected int freeFromM(int freeNum)
        {
            if (freeNum <= 0 || Count <= 0)
                return 0;

            if (freeNum >= Count)
            {
                freeNum = Count;
                mYinYang = -1;
            }
            else
            {
                mYinYang = 0;
            }

            moveReverseClockwise(ref mMIndex, freeNum);

            return freeNum;
        }

        // Growed Reverse Clockwise direction(negtive order in array index, defined as Reverse Clockwise direction)
        public bool rPush(T item)
        {
            if (NFree < 1 && allocForN(1) <= 0)
                return false;

            moveReverseClockwise(ref mNIndex, 1);

            if (mNIndex == mMIndex)
                mYinYang = 1;
            else
                mYinYang = 0;

            mItemList[mNIndex] = item;

            return true;
        }

        public bool rPop(out T item)
        {
            item = default(T);

            if (Count <= 0)
                return false;

            item = mItemList[mNIndex];
            mItemList[mNIndex] = default(T);

            freeFromN(1);

            return true;
        }

        public void rPop()
        {
            if (Count <= 0)
                return;

            mItemList[mNIndex] = default(T);

            freeFromN(1);
        }

        // Growed positive direction(positive order in array index, defined as Clockwise direction)
        public bool Push(T item)
        {
            if (MFree < 1 && allocForM(1) <= 0)
                return false;

            mItemList[mMIndex] = item;

            moveClockwise(ref mMIndex, 1);

            if (mNIndex == mMIndex)
                mYinYang = 1;
            else
                mYinYang = 0;

            return true;
        }

        public bool Pop(out T item)
        {
            item = default(T);

            if (Count <= 0)
                return false;

            freeFromM(1);
            item = mItemList[mMIndex];
            mItemList[mMIndex] = default(T);
            return true;
        }

        public void Pop()
        {
            if (Count <= 0)
                return;

            freeFromM(1);
            mItemList[mMIndex] = default(T);
            return;
        }

        public bool Enqueue(T item)
        {
            return Push(item);
        }

        public bool Dequeue(out T item)
        {
            return rPop(out item);
        }

        public void DequeueAndDrop()
        {
            rPop();
        }

        public bool rEnqueue(T item)
        {
            return rPush(item);
        }

        public bool rDequeue(out T item)
        {
            return Pop(out item);
        }

        public bool DequeueAt(int offset, out T item)
        {
            item = default(T);
            if (Count <= 0 || offset >= Count)
                return false;

            item = this[offset];

            for (int i = offset; i > 0; i--)
                this[i] = this[i - 1];

            rPop();

            return true;
        }

        public bool rDequeueAt(int offset, out T item)
        {
            item = default(T);
            if (Count <= 0 || offset >= Count)
                return false;

            offset = Count - offset - 1;
            item = this[offset];

            for (int i = offset; i < Count - 1; i++)
                this[i] = this[i + 1];

            Pop();
            return true;
        }

        protected int indexFromNOffset(int offset)
        {
            if (offset >= Count)
                throw new System.IndexOutOfRangeException();

            int index = mNIndex;
            if (offset == 0)
                return index;

            index = (mNIndex + offset) % mCapacity;
            //if (index >= mMIndex)
            //    throw new System.IndexOutOfRangeException();

            return index;
        }

        protected int indexFromMOffset(int offset)
        {
            if (offset >= Count)
                throw new System.IndexOutOfRangeException();

            int index = mMIndex - 1 - offset;
            if (index < 0)
                index += mCapacity;

            //if (index < mNIndex && mNIndex == mMIndex)
            //    throw new System.IndexOutOfRangeException();

            return index;
        }

        public T AtOffset(int offset)
        {
            int index = indexFromNOffset(offset);
            return mItemList[index];
        }

        public T AtOffsetR(int offset)
        {
            int index = indexFromMOffset(offset);
            return mItemList[index];
        }

        public T AtFixOffset(int offset)
        {
            if (offset < 0 || offset > mItemList.Length)
                throw new System.IndexOutOfRangeException();

            return mItemList[offset];
        }

        public int NowMinFixIndex()
        {
            if (Count <= 0)
                return -1;

            return indexFromNOffset(0);
        }

        public T this[int i]
        {
            get { int index = indexFromNOffset(i); return mItemList[index]; }
            set { int index = indexFromNOffset(i); mItemList[index] = value; }
        }

        public IEnumerable<T> Enumrate()
        {
            if (Count <= 0)
                yield break;
            int count = Count;
            for (int i = indexFromNOffset(0), c = 0; c < count; moveClockwise(ref i, 1), c++)
                yield return mItemList[i];
        }

        public IEnumerable<T> rEnumrate()
        {
            if (Count <= 0)
                yield break;
            var count = Count;
            for (int i = indexFromMOffset(0), c = 0; c < count; moveReverseClockwise(ref i, 1), c++)
                yield return mItemList[i];
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Count <= 0)
                yield break;
            int count = Count;
            for (int i = indexFromNOffset(0), c = 0; c < count; moveClockwise(ref i, 1), c++)
                yield return mItemList[i];
        }
    }
}