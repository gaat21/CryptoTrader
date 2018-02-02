-- -----------------------------------------------------
-- Schema identity
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `trading` DEFAULT CHARACTER SET utf8;
USE `trading`;

-- -----------------------------------------------------
-- Table 'candles'
-- -----------------------------------------------------

CREATE TABLE IF NOT EXISTS `candles` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `ScanId` int(11) NOT NULL,
  `StartDateTime` datetime DEFAULT NULL,
  `HighPrice` decimal(16,10) DEFAULT NULL,
  `OpenPrice` decimal(16,10) DEFAULT NULL,
  `ClosePrice` decimal(16,10) DEFAULT NULL,
  `LowPrice` decimal(16,10) DEFAULT NULL,
  `Volume` decimal(16,10) DEFAULT NULL,
  `VolumeWeightedPrice` decimal(16,10) DEFAULT NULL,
  `TradingPair` varchar(50) DEFAULT NULL,
  `TradingCount` int(10) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
