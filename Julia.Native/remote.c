#include <stm32f4xx_gpio.h>
#include <stm32f4xx_tim.h>
#include <stm32f4xx_exti.h>
#include <stm32f4xx_rcc.h>
#include <stm32f4xx_syscfg.h>
#include "remote.h"

extern void OnRemoteControlButtonPressed(uint8_t address, uint8_t button);

void remoteInit(void)
{
	TIM_TimeBaseInitTypeDef TIM_TimeBaseStructure; 
	NVIC_InitTypeDef NVIC_InitStructure;
	GPIO_InitTypeDef GPIO_InitStructure;
	EXTI_InitTypeDef EXTI_InitStructure;

	/* Enable GPIOA clock */
	RCC_AHB1PeriphClockCmd(RCC_AHB1Periph_GPIOA, ENABLE);
	/* Enable SYSCFG clock */
	RCC_APB2PeriphClockCmd(RCC_APB2Periph_SYSCFG, ENABLE);
	
	/* Configure PA0 pin as input floating */
	GPIO_InitStructure.GPIO_Mode = GPIO_Mode_IN;
	GPIO_InitStructure.GPIO_PuPd = GPIO_PuPd_NOPULL;
	GPIO_InitStructure.GPIO_Pin = GPIO_Pin_0;
	GPIO_Init(GPIOA, &GPIO_InitStructure);
	
	/* Connect EXTI Line0 to PA0 pin */
	SYSCFG_EXTILineConfig(EXTI_PortSourceGPIOA, EXTI_PinSource0);
	
	/* Configure EXTI Line0 */
	EXTI_InitStructure.EXTI_Line = EXTI_Line0;
	EXTI_InitStructure.EXTI_Mode = EXTI_Mode_Interrupt;
	EXTI_InitStructure.EXTI_Trigger = EXTI_Trigger_Rising_Falling;  
	EXTI_InitStructure.EXTI_LineCmd = ENABLE;
	EXTI_Init(&EXTI_InitStructure);
	
	/* Enable and set EXTI Line0 Interrupt to the lowest priority */
	NVIC_InitStructure.NVIC_IRQChannel = EXTI0_IRQn;
	NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 1;
	NVIC_InitStructure.NVIC_IRQChannelSubPriority = 1;
	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
	NVIC_Init(&NVIC_InitStructure);

	/* Enable the TIM2 gloabal Interrupt */
	NVIC_InitStructure.NVIC_IRQChannel = TIM2_IRQn;
	NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 0;
	NVIC_InitStructure.NVIC_IRQChannelSubPriority = 1;
	NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
	NVIC_Init(&NVIC_InitStructure);

	RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM2, ENABLE);
	
	/* Time base configuration 42MHz*/
	TIM_TimeBaseStructure.TIM_Period = 56 - 1; // 56 usec period
	TIM_TimeBaseStructure.TIM_Prescaler = 84 - 1; // down to 1 MHz
	TIM_TimeBaseStructure.TIM_ClockDivision = TIM_CKD_DIV1;
	TIM_TimeBaseStructure.TIM_CounterMode = TIM_CounterMode_Up;
	TIM_TimeBaseInit(TIM2, &TIM_TimeBaseStructure);
	/* TIM IT enable */
	TIM_ITConfig(TIM2, TIM_IT_Update, ENABLE);
	/* TIM2 enable counter */
	TIM_Cmd(TIM2, DISABLE);
}

#define false 0
#define true 1

static volatile uint8_t time = 0;

//timer interrupt. should be every 56 usec
void TIM2_IRQHandler(void)
{
	if (TIM_GetITStatus(TIM2, TIM_IT_Update) == RESET) return;
	time++;
	TIM_ClearITPendingBit(TIM2, TIM_IT_Update);
}

static uint8_t dataValid(uint32_t data)
{
	uint8_t Addr, nAddr, Cmd, nCmd;
	Addr = (data >> (8 * 3)) & 0xFF;
	nAddr = ((~data) >> (8 * 2)) & 0xFF;
	Cmd = (data >> (8 * 1)) & 0xFF;
	nCmd = ((~data) >> (8 * 0)) & 0xFF;
	return (Addr == nAddr) && (Cmd == nCmd);
}

#define DELTA(x)	(x * 3 / 10)
#define LOW(x)		(x - DELTA(x))
#define HIGH(x)		(x + DELTA(x))

void EXTI0_IRQHandler(void)
{
	static uint8_t status;
	static uint32_t data;
	static uint8_t nextOneIsLast;
	static uint8_t inReceive = false;

	if(EXTI_GetITStatus(EXTI_Line0) == RESET) return;

	if (!(GPIOA->IDR & 0x01))
	{
		if (!inReceive)
		{
			time = 0;
			TIM_Cmd(TIM2, ENABLE);
			inReceive = true;
			status = 0;
			data = 0;
			nextOneIsLast = false;
		}
		else
			switch (status)
			{
				case 1:
					if (time >= LOW(80) && time <= HIGH(80))
					{
						status = 2;
					}
					else if (time >= LOW(40) && time <= HIGH(40))
					{
						//this was a repeat signal
						data = 0xFF00FF00;
						nextOneIsLast = true;
						status = 2;
					}
					else
					{
						inReceive = false;
						TIM_Cmd(TIM2, DISABLE);
					}
					break;

				default:
					data <<= 1;
					if (time >= LOW(10) && time <= HIGH(10))
					{
					 	//logical 0, do nothing
					}
					else if (time >= LOW(30) && time <= HIGH(30))
					{
						data |= 1;
					}
					else
					{
						//invalid bit received!
						inReceive = false;
						TIM_Cmd(TIM2, DISABLE);
					}
					
					status++;
					if (status == 34)
					{
						nextOneIsLast = true;
					}
					break;
			}
	}
	else
	{
		switch (status)
		{
			case 0:
				if (time < LOW(160) || time > HIGH(160))
				{
					inReceive = false;
					TIM_Cmd(TIM2, DISABLE);
				}
				status = 1;
				break;

			default:
				if (time < LOW(10) || time > HIGH(10))
				{
					inReceive = false;
					TIM_Cmd(TIM2, DISABLE);
				}

				if (nextOneIsLast)
				{
					inReceive = false;
					TIM_Cmd(TIM2, DISABLE);
					
					if (dataValid(data))
					{
						OnRemoteControlButtonPressed((data >> (8 * 3)) & 0xFF, (data >> (8 * 1)) & 0xFF);
					}
				}
				break;
		}
	}
	time = 0;

	EXTI_ClearITPendingBit(EXTI_Line0);
}
