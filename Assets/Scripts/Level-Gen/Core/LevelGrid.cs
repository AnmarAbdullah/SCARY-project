using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    public class LevelGrid
    {
        public readonly int width;
        public readonly int height;
        private readonly TileInstance[,] cells;

        public LevelGrid(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.cells = new TileInstance[width, height];
        }

        public Vector2Int Center => new Vector2Int(width / 2, height / 2);

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
        }

        public TileInstance Get(Vector2Int cell)
        {
            if (!InBounds(cell)) return null;
            return cells[cell.x, cell.y];
        }

        public void Set(Vector2Int cell, TileInstance tile)
        {
            cells[cell.x, cell.y] = tile;
        }

        public bool IsEmpty(Vector2Int cell)
        {
            return InBounds(cell) && cells[cell.x, cell.y] == null;
        }

        public int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        public IEnumerable<Vector2Int> AllCells()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    yield return new Vector2Int(x, y);
        }

        public IEnumerable<Vector2Int> CellsByCategory(TileCategory category)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var t = cells[x, y];
                    if (t != null && t.definition != null && t.definition.category == category)
                        yield return new Vector2Int(x, y);
                }
        }

        public IEnumerable<Vector2Int> CellsByDefinition(TileDefinition def)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var t = cells[x, y];
                    if (t != null && t.definition == def)
                        yield return new Vector2Int(x, y);
                }
        }

        public int CountByDefinition(TileDefinition def)
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var t = cells[x, y];
                    if (t != null && t.definition == def) count++;
                }
            return count;
        }
    }
}
