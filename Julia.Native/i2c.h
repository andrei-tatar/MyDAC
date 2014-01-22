#ifndef _I2C_H_
#define _I2C_H_

void i2cInit(void);

void i2cWrite(uint8_t slaveAddress, const uint8_t *data, int length);
void i2cRead(uint8_t slaveAddress, uint8_t *data, int length);

#endif
