using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Laba2
{
    public interface IVirtualArray
    {
        void Create(string fileName, long size, int maxStrSize = 0);
        void Open(string fileName);
        void Input(long index, object value);
        object Print(long index);
        void Close();
    }

    public interface ICreator { }

    public abstract partial class VirtualMemoryArray
    {
        protected FileStream fs;
        protected List<Page> buffer = new();
        protected int maxBufferPages = Constants.BUFFER_SIZE;
        protected long arraySize;
        protected int elementsPerPage = Constants.ELEMS_PER_PAGE;

        // Размер одного элемента в байтах (должен быть задан в производном классе)
        protected abstract int ElementSize { get; }

        // Загрузка страницы из файла (специфична для типа)
        protected virtual Page LoadPageFromFile(int pageNumber)
        {
            throw new NotImplementedException();
        }

        // Сохранение страницы в файл
        protected virtual void SavePageToFile(Page page)
        {
            throw new NotImplementedException();
        }

        // Поиск индекса страницы в буфере, подгрузка при необходимости
        protected int? FindPageIndex(long elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= arraySize)
                return null;

            int pageNumber = (int)(elementIndex / elementsPerPage);

            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i].pageNumber == pageNumber)
                {
                    buffer[i].lastAccess = DateTime.Now;
                    return i;
                }
            }

            try
            {
                Page newPage = null;

                if (buffer.Count < maxBufferPages)
                {
                    newPage = LoadPageFromFile(pageNumber);
                    newPage.lastAccess = DateTime.Now;
                    newPage.modified = false;
                    buffer.Add(newPage);
                    return buffer.Count - 1;
                }

                // Выбор страницы для замещения (самая старая)
                Page victim = null;
                int victimIndex = -1;
                DateTime oldest = DateTime.MaxValue;

                for (int i = 0; i < buffer.Count; i++)
                {
                    if (buffer[i].lastAccess < oldest)
                    {
                        oldest = buffer[i].lastAccess;
                        victim = buffer[i];
                        victimIndex = i;
                    }
                }

                if (victim.modified)
                {
                    SavePageToFile(victim);
                }

                buffer.RemoveAt(victimIndex);

                newPage = LoadPageFromFile(pageNumber);
                newPage.lastAccess = DateTime.Now;
                newPage.modified = false;
                buffer.Add(newPage);
                return buffer.Count - 1;
            }
            catch
            {
                return null;
            }
        }

        // Чтение байтов элемента (предполагается, что страница уже в буфере)
        protected byte[] ReadElementBytes(long index)
        {
            int? pageIdx = FindPageIndex(index);
            if (pageIdx == null)
                throw new InvalidOperationException("Cannot access page");

            Page page = buffer[pageIdx.Value];
            int inPageIndex = (int)(index % elementsPerPage);
            int byteOffset = inPageIndex * ElementSize;

            byte[] result = new byte[ElementSize];
            Array.Copy(page.data, byteOffset, result, 0, ElementSize);
            return result;
        }

        // Запись байтов элемента
        protected void WriteElementBytes(long index, byte[] data)
        {
            if (data.Length != ElementSize)
                throw new ArgumentException("Data size does not match element size");

            int? pageIdx = FindPageIndex(index);
            if (pageIdx == null)
                throw new InvalidOperationException("Cannot access page");

            Page page = buffer[pageIdx.Value];
            int inPageIndex = (int)(index % elementsPerPage);
            int byteOffset = inPageIndex * ElementSize;

            Array.Copy(data, 0, page.data, byteOffset, ElementSize);
            page.modified = true;

            // Устанавливаем бит в битовой карте
            int byteIdx = inPageIndex / 8;
            int bitIdx = inPageIndex % 8;
            page.bitmap[byteIdx] |= (byte)(1 << bitIdx);
        }

        // Проверка, был ли элемент записан (по битовой карте)
        protected bool IsElementWritten(long index)
        {
            int? pageIdx = FindPageIndex(index);
            if (pageIdx == null)
                throw new InvalidOperationException("Cannot access page");

            Page page = buffer[pageIdx.Value];
            int inPageIndex = (int)(index % elementsPerPage);
            int byteIdx = inPageIndex / 8;
            int bitIdx = inPageIndex % 8;
            return (page.bitmap[byteIdx] & (1 << bitIdx)) != 0;
        }

        // Сохранение всех изменённых страниц и закрытие файла
        public virtual void Close()
        {
            foreach (var page in buffer)
            {
                if (page.modified)
                    SavePageToFile(page);
            }
            buffer.Clear();
            fs?.Close();
        }
    }
}