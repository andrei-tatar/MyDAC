#ifndef _USART_H_
#define _USART_H_

#include <stdint.h>

void usartInit(uint32_t baudrate);
void usartSendPacket(uint8_t cmd, const uint8_t *data, int length);

void ProcessPacket(uint8_t cmd, uint8_t *data, int length);

#endif
