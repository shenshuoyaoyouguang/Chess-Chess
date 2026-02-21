#ifndef SHARK_ENGINE_EVALUATION_H
#define SHARK_ENGINE_EVALUATION_H

#include "Types.h"
#include "Board.h"

namespace SharkEngine {

// 评估类
class Evaluation {
public:
    // 评估棋局（返回红方视角的分值，单位：厘兵）
    static int evaluate(const Board& board);
    
private:
    // 棋子价值（基础）
    static constexpr int PIECE_VALUES[] = {
        0,      // NONE
        10000,  // KING (将/帅) - 极高价值
        200,    // ADVISOR (士/仕)
        200,    // BISHOP (象/相)
        400,    // KNIGHT (马)
        900,    // ROOK (车)
        450,    // CANNON (炮)
        100     // PAWN (兵/卒)
    };
    
    // 位置价值表（红方视角）
    static const int PAWN_POSITION_VALUES[10][9];
    static const int KNIGHT_POSITION_VALUES[10][9];
    static const int CANNON_POSITION_VALUES[10][9];
    static const int ROOK_POSITION_VALUES[10][9];
    
    // 评估组件
    static int materialScore(const Board& board);
    static int positionScore(const Board& board);
    static int mobilityScore(const Board& board);
    static int kingSafetyScore(const Board& board);
    
    // 单棋子评估
    static int pawnScore(Square sq, Color color);
    static int knightScore(Square sq, Color color);
    static int cannonScore(Square sq, Color color);
    static int rookScore(Square sq, Color color);
    
    // 辅助
    static int mirrorRank(int rank);  // 翻转等级（用于黑方位置表）
};

} // namespace SharkEngine

#endif // SHARK_ENGINE_EVALUATION_H
