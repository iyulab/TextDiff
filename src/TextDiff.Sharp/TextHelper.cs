namespace TextDiff
{
    public static class TextHelper
    {
        /// <summary>
        /// 텍스트를 줄 단위로 분할합니다.
        /// </summary>
        /// <param name="text">분할할 텍스트</param>
        /// <returns>줄의 리스트</returns>
        public static List<string> SplitLines(string text)
        {
            return string.IsNullOrEmpty(text)
                ? new List<string>()
                : text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
        }
    }
}
