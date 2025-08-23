# Scripts de Teste de Quantização de Posição

Este documento descreve como usar os scripts criados para testar visualmente o impacto da quantização de posições em diferentes tipos de dados.

## Scripts Criados

### 1. PositionQuantizationTest
**Arquivos:** `PositionQuantizationTest.h` e `PositionQuantizationTest.cpp`

Este script permite visualizar em tempo real como diferentes tipos de quantização afetam a precisão de uma posição específica.

#### Funcionalidades:
- Compara posição original (float32) com versões quantizadas
- Suporte para: Float16, Int32, Int16, Int8
- Visualização com esferas coloridas para cada tipo
- Linhas de erro mostrando desvio da posição original
- Texto com estatísticas de erro em tempo real

#### Como Usar:
1. Adicione o ator `APositionQuantizationTest` à sua cena
2. Configure o campo `TrackedActor` para apontar para um ator que você quer rastrear
3. Ajuste os parâmetros de quantização:
   - `Int32Range`: Faixa de valores para Int32 (padrão: ±1000 unidades)
   - `Int16Range`: Faixa de valores para Int16 (padrão: ±100 unidades)  
   - `Int8Range`: Faixa de valores para Int8 (padrão: ±10 unidades)
4. Configure as opções visuais (cores, tamanho das esferas, etc.)

#### Interpretação dos Resultados:
- **Esfera Branca**: Posição original (Float32)
- **Esfera Verde**: Float16 - boa precisão, pequeno ganho de espaço
- **Esfera Azul**: Int32 - precisão excelente dentro da faixa definida
- **Esfera Amarela**: Int16 - precisão moderada, bom compromisso espaço/precisão
- **Esfera Vermelha**: Int8 - baixa precisão, máximo ganho de espaço

### 2. NetworkMovementSimulator
**Arquivos:** `NetworkMovementSimulator.h` e `NetworkMovementSimulator.cpp`

Este script simula movimento contínuo e mostra como erros de quantização se acumulam ao longo do tempo, simulando condições reais de rede.

#### Funcionalidades:
- Movimento circular automático para simular jogador em movimento
- Simulação de taxa de atualização de rede (Hz)
- Trajetória visual mostrando caminho original vs quantizado
- Estatísticas acumulativas de erro
- Comparação de diferentes tipos de quantização

#### Como Usar:
1. Adicione o ator `ANetworkMovementSimulator` à sua cena
2. Configure os parâmetros de simulação:
   - `NetworkUpdateRate`: Taxa de atualização (padrão: 20 Hz)
   - `SimulationSpeed`: Velocidade do movimento (unidades/segundo)
   - `MovementRadius`: Raio do movimento circular
3. Escolha o tipo de quantização a testar em `TestQuantizationType`
4. Use `ResetStatistics()` para reiniciar os dados

#### Interpretação dos Resultados:
- **Linha Verde**: Trajetória original (Float32)
- **Linha Vermelha**: Trajetória quantizada
- **Linhas Laranja**: Mostram erro instantâneo entre posições
- **Texto**: Estatísticas em tempo real (erro médio, máximo, total)

## Recomendações de Uso

### Para Diferentes Cenários:

#### 1. Jogos de Combate/Precisão (FPS, Hack & Slash)
- **Recomendado**: Int16 com range de ±100-200m
- **Precisão**: ~0.003m (3mm) - suficiente para gameplay preciso
- **Economia**: 50% do espaço comparado a Float32

#### 2. Jogos de Mundo Aberto (MMO, Survival)
- **Recomendado**: Int32 com range de ±1000-5000m  
- **Precisão**: Excelente dentro da área de jogo
- **Economia**: Mesma precisão que Float32, mas range limitado

#### 3. Jogos Casuais/Mobile
- **Recomendado**: Int16 com range de ±50-100m
- **Precisão**: ~0.0015m (1.5mm) - mais que suficiente
- **Economia**: 50% do espaço + melhor para dispositivos móveis

#### 4. Objetos Distantes/Menos Importantes
- **Recomendado**: Int8 com range adaptativo
- **Precisão**: ~0.08m (8cm) - adequado para objetos secundários
- **Economia**: 75% do espaço

### Dicas de Otimização:

1. **Range Adaptativo**: Ajuste o range baseado na área de jogo atual
2. **Quantização Hierárquica**: Use diferentes precisões para diferentes tipos de objetos
3. **Compressão Temporal**: Considere usar delta compression junto com quantização
4. **Teste com Movimento Real**: Use padrões de movimento dos seus jogadores reais

### Limitações dos Tipos:

#### Float16:
- ✅ Boa precisão geral
- ❌ Ainda usa bastante espaço
- ❌ Pode ter problemas com valores muito grandes ou pequenos

#### Int32:
- ✅ Precisão excelente dentro do range
- ✅ Previsível e determinístico
- ❌ Range limitado
- ❌ Mesmo tamanho que Float32

#### Int16:
- ✅ Bom compromisso espaço/precisão
- ✅ 50% menor que Float32
- ❌ Range mais limitado
- ❌ Pode ser insuficiente para mundos muito grandes

#### Int8:
- ✅ Máxima economia de espaço (75% menor)
- ✅ Ideal para dispositivos móveis
- ❌ Precisão limitada
- ❌ Range muito pequeno

## Compilação

Certifique-se de que os arquivos estão incluídos no seu projeto Unreal:
1. Adicione os arquivos ao seu módulo `ToS_Network`
2. Recompile o projeto
3. Os atores estarão disponíveis na categoria "Tools" no editor

## Próximos Passos

Considere implementar:
1. **Testes de Latência**: Simular delay de rede junto com quantização
2. **Interpolação**: Testar como diferentes tipos afetam suavização de movimento
3. **Compressão**: Combinar quantização com algoritmos de compressão
4. **Métricas de Gameplay**: Medir impacto real na jogabilidade
