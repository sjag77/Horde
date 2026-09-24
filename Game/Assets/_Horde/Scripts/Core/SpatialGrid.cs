using UnityEngine;

namespace Horde
{
    /// <summary>
    /// Uniform grid over the arena storing enemy indices as per-cell linked lists.
    /// Rebuilt every frame in O(n); hit and separation queries only touch nearby cells.
    /// </summary>
    public sealed class SpatialGrid
    {
        public readonly float CellSize;
        public readonly int Cols, Rows;
        readonly float minX, minY;
        readonly int[] head;
        readonly int[] next;

        public SpatialGrid(float halfW, float halfH, float margin, float cellSize, int capacity)
        {
            CellSize = cellSize;
            minX = -halfW - margin;
            minY = -halfH - margin;
            Cols = Mathf.CeilToInt((halfW + margin) * 2f / cellSize);
            Rows = Mathf.CeilToInt((halfH + margin) * 2f / cellSize);
            head = new int[Cols * Rows];
            next = new int[capacity];
            Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < head.Length; i++) head[i] = -1;
        }

        public int CellX(float x) => Mathf.Clamp((int)((x - minX) / CellSize), 0, Cols - 1);
        public int CellY(float y) => Mathf.Clamp((int)((y - minY) / CellSize), 0, Rows - 1);

        public void Insert(int index, Vector2 p)
        {
            int cell = CellY(p.y) * Cols + CellX(p.x);
            next[index] = head[cell];
            head[cell] = index;
        }

        public int Head(int cx, int cy) => head[cy * Cols + cx];
        public int Next(int index) => next[index];
    }
}
