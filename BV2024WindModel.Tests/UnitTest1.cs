using BV2024WindModel.Abstractions;
using BV2024WindModel.Data;
using BV2024WindModel.Logic;
using Newtonsoft.Json;

namespace BV2024WindModel.Tests
{
    [TestClass]
    public class BV2024UnitTest1
    {
        [TestMethod]
        [DataRow("C:\\windLoadFiles\\wind9.csv", "C:\\windLoadFiles\\wind9ReferenceResults1.txt")]
        [DataRow("C:\\windLoadFiles\\wind7.csv", "C:\\windLoadFiles\\wind7ReferenceResults1.txt")]
        public void TestMethod1(string inputFileName, string inputReferenceResultsFileName)
        {
            var containersFromFile = ReadCSV.ReadFromCsv(inputFileName);

            var calculator = new BV2024WindCalculator();

            var windExposedFrontSurfaces = calculator.Calculate(containersFromFile);

            var windCalculationResults = WindCalculationResultFactory.Create(windExposedFrontSurfaces).OrderBy(entry => entry.Coordinate).ToList();

            string referenceWindResultsSerialized = File.ReadAllText(inputReferenceResultsFileName);

            var referenceResults = (JsonConvert.DeserializeObject<List<WindCalculationResult>>(referenceWindResultsSerialized)).OrderBy(entry => entry.Coordinate).ToList();

            Assert.IsNotNull(referenceResults);
            Assert.IsNotNull(windCalculationResults);
            Assert.AreEqual(windCalculationResults.Count, referenceResults.Count);
            //foreach (var referenceResult in referenceResults)
            for ( var index=0; index < referenceResults.Count; index++)
            {
                var referenceResult = referenceResults[index];
                var actualRecord = windCalculationResults.FirstOrDefault(entry => Math.Abs(entry.Coordinate - referenceResult.Coordinate) < 0.001);
                Assert.IsNotNull(actualRecord);
                var difference = Math.Abs(actualRecord.Area - referenceResult.Area);
                Assert.IsTrue(difference < 1e-0, $"For result with index {index} difference is {difference}");
            }

        }

    }
}