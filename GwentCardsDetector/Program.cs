using GwentCardsDetector;

SingleCardDetector.Detect("20241102_231546.jpg");

//bool run = true;
//while (run)
//{
//    await Console.Out.WriteLineAsync("Enter the path to the image you want to analyze:");
//    await Console.Out.WriteAsync(">>> ");
//    string path = Console.ReadLine();

//    if (!File.Exists(path))
//    {
//        await Console.Out.WriteLineAsync("\nThe file does not exist!\n");
//        continue;
//    }

//    await Console.Out.WriteLineAsync("Detecting cards...");
//    MultipleCardsDetector.Detect(path);
//}