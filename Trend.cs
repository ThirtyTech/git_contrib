public static class Trend
{
    public static string GenerateTrendLine(int[] numbers)
    {
        int firstNumber = numbers[0];
        int lastNumber = numbers[numbers.Length - 1];
        double average = numbers.Average();
        int maxNumber = numbers.Max();
        double threshold = maxNumber * 0.5; // 50% of the maximum number

        if (average < threshold)
        {
            return "▄▄▄";
        }
        else if (lastNumber > firstNumber)
        {
            return "▄■▀";
        }
        else if (lastNumber < firstNumber)
        {
            return "▀■▄";
        }
        else
        {
            return "■■■";
        }
    }
}


