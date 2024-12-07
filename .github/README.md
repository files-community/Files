#pragma once

static void InfinitoQualquerCoisa(uintptr QualquerCoisa, uint valor)
{
	*QualquerCoisa = valor;
}

inline static void funcAimBot(Jogadores Entidades[], Jogadores* MeuJogador)
{
	if (*(Entidades[0].PegptrVida()) < 1)	// Testa se eu estou vivo
		return;

	short JIndex = -1;

	ushort Vida0 = 0;

	float tempHip[2] = { 0, 0 };	// hip horizontal, hip vertical
	float tempDifVec3[3] = { 0, 0, 0 };

	// Algorítimo de melhor alvo
	for (ushort i = 1; i < Geral::Num_Jogadores; i++)
	{
		if (*(Entidades[i].PegptrVida()) < 0 || *(Entidades[i].PegptrVida()) > 100)
		{
			++Vida0;
			continue;
		}

		Vida0 = 0;

		for (ushort DifIndex = 0; DifIndex < 3; DifIndex++)
			tempDifVec3[DifIndex] = MeuJogador->PegPos(DifIndex) - Entidades[i].PegPos(DifIndex);

		tempHip[0] = hypotf(tempDifVec3[0], tempDifVec3[1]);
		tempHip[1] = hypotf(tempDifVec3[2], tempHip[0]);

		if (JIndex == -1 || tempHip[0] + tempHip[1] < Geral::MenorHip[0] + Geral::MenorHip[1])
		{
			JIndex = i;
			Geral::MenorHip[0] = tempHip[0];
			Geral::MenorHip[1] = tempHip[1];

			for (ushort DifIndex = 0; DifIndex < 3; DifIndex++)
				Geral::DifVec3[DifIndex] = tempDifVec3[DifIndex];
		}
	}

	if (Vida0 == Geral::Num_Jogadores - 1)
		return;

	// Ang horizontal
	float yaw = -atanf(Geral::DifVec3[0] / Geral::DifVec3[1]) * (180.0f / (float)MEUPI);

	if (Geral::DifVec3[1] < 0.0f)
		yaw += 180.0f;

	while (yaw < 0.0f)
		yaw += 360.0f;
	while (yaw > 360.0f)
		yaw -= 360.0f;

	MeuJogador->DefAng(yaw, 0);

	// Ang vertical
	MeuJogador->DefAng((-asin(Geral::DifVec3[2] / Geral::MenorHip[1]) * (180.0f / (float)MEUPI)) + 0.6f, 1);

	// Jogador sendo mirado
	// Quando eu usar OpenGl ou DirectX
}