using System.Collections.Generic;
using System.Linq;

namespace DVDispatcherMod.DispatcherHints {
    public static class AdjacentCarsGrouper {
        public static List<List<TrainCar>> GetGroups(IEnumerable<TrainCar> trainCars) {
            return trainCars.OrderBy(c => c.indexInTrainset).Aggregate(new List<List<TrainCar>>(), AddCarToAdjacencyAggregatedCars);
        }

        private static List<List<TrainCar>> AddCarToAdjacencyAggregatedCars(List<List<TrainCar>> carGroups, TrainCar trainCar) {
            if (carGroups.Any()) {
                var lastCarGrop = carGroups.Last();
                var lastTrainCar = lastCarGrop.Last();
                if (lastTrainCar.indexInTrainset + 1 == trainCar.indexInTrainset) {
                    lastCarGrop.Add(trainCar);
                    return carGroups;
                }
            }

            carGroups.Add(new List<TrainCar> { trainCar });

            return carGroups;
        }
    }
}