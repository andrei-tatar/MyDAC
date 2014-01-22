#ifndef _OLED_H_
#define _OLED_H_

#include <stdint.h>

void oledInit(void);
void oledTurnDisplay(uint8_t turnOn);
void oledSetBrightness(uint8_t brightness);
void oledFlush(const uint8_t *data);

#endif
