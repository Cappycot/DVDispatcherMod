using System;
using System.Collections.Generic;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHints {
    public static class AdjacentCarsCenterFinder {
        public static Vector3 FindCenter(List<TrainCar> cars) {
            var carCount = cars.Count;
            if (carCount == 0) {
                throw new ArgumentException("cannot get attention point from empty list of cars");
            } else if ((carCount % 2) == 0) {
                var car1 = cars[carCount / 2 - 1];
                var car2 = cars[carCount / 2];

                FindCoupledCouplerPair(car1, car2, out var car1Coupler, out var car2Coupler);

                return Vector3.Lerp(car1Coupler.transform.position, car2Coupler.transform.position, 0.5f);
            } else {
                var car = cars[carCount / 2];
                return Vector3.Lerp(car.frontCoupler.transform.position, car.rearCoupler.transform.position, 0.5f);
            }
        }

        private static void FindCoupledCouplerPair(TrainCar car1, TrainCar car2, out Coupler car1Coupler, out Coupler car2Coupler) {
            if (car1.frontCoupler.coupledTo == car2.frontCoupler) {
                car1Coupler = car1.frontCoupler;
                car2Coupler = car2.frontCoupler;
            } else if (car1.rearCoupler.coupledTo == car2.frontCoupler) {
                car1Coupler = car1.rearCoupler;
                car2Coupler = car2.frontCoupler;
            } else if (car1.frontCoupler.coupledTo == car2.rearCoupler) {
                car1Coupler = car1.frontCoupler;
                car2Coupler = car2.rearCoupler;
            } else if (car1.rearCoupler.coupledTo == car2.rearCoupler) {
                car1Coupler = car1.rearCoupler;
                car2Coupler = car2.rearCoupler;
            } else {
                throw new InvalidOperationException("could not find common couplers for adjacent cars");
            }
        }
    }
}