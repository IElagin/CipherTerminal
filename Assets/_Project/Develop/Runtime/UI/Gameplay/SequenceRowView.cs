using UnityEngine;

namespace Assets._Project.Develop.Runtime.UI.Gameplay
{
    public sealed class SequenceRowView : MonoBehaviour
    {
        private const float AvailableWidth = 1280;
        private const float Spacing = 18;
        private const float MaximumCellWidth = 190;
        private const float CellHeight = 112;
        private const float CenterFraction = .5f;
        private const int TrailingGapCount = 1;

        [SerializeField] private SequenceCellView[] _cells;
        [SerializeField] private bool _showMarkers;

        public void Render(string symbols, int length, int verified, int errorIndex, int cursorIndex)
        {
            float width = Mathf.Min(MaximumCellWidth, (AvailableWidth - (length - TrailingGapCount) * Spacing) / length);

            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].gameObject.SetActive(i < length);

                if (i >= length)
                    continue;

                var rect = (RectTransform)_cells[i].transform;
                rect.sizeDelta = new Vector2(width, CellHeight);
                rect.anchoredPosition = new Vector2(GetPosition(i, length), 0);
                bool done = i < verified;
                bool error = i == errorIndex;
                bool current = i == cursorIndex;
                string symbol = i < symbols.Length ? (symbols[i] == ' ' ? "␣" : symbols[i].ToString()) : "";
                string marker = _showMarkers ? (error ? "ОШИБКА" : done ? "ВЕРНО" : current ? "ВВОД" : "") : "";
                _cells[i].Render(symbol, marker, done, current, error);
            }
        }

        public float GetPosition(int index, int length)
        {
            float width = Mathf.Min(MaximumCellWidth, (AvailableWidth - (length - TrailingGapCount) * Spacing) / length);
            float total = length * (width + Spacing) - Spacing;
            return -total * CenterFraction + width * CenterFraction + index * (width + Spacing);
        }
    }
}
