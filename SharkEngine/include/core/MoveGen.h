#ifndef SHARK_ENGINE_MOVEGEN_H
#define SHARK_ENGINE_MOVEGEN_H

#include "Types.h"
#include "Board.h"
#include <vector>

namespace SharkEngine {

// 走棋生成器
class MoveGenerator {
public:
    // 生成所有合法走法
    static std::vector<Move> generateLegalMoves(const Board& board);
    
    // 生成所有伪合法走法（不检查将军）
    static std::vector<Move> generatePseudoLegalMoves(const Board& board);
    
    // 生成吃子走法
    static std::vector<Move> generateCaptureMoves(const Board& board);
    
    // 生成非吃子走法
    static std::vector<Move> generateQuietMoves(const Board& board);
    
    // 检查走法是否合法
    static bool isLegal(const Board& board, const Move& move);
    
    // 检查是否将军
    static bool givesCheck(const Board& board, const Move& move);

private:
    // 各棋子走法生成
    static void generatePawnMoves(const Board& board, Square from, std::vector<Move>& moves);
    static void generateKnightMoves(const Board& board, Square from, std::vector<Move>& moves);
    static void generateBishopMoves(const Board& board, Square from, std::vector<Move>& moves);
    static void generateRookMoves(const Board& board, Square from, std::vector<Move>& moves);
    static void generateCannonMoves(const Board& board, Square from, std::vector<Move>& moves);
    static void generateAdvisorMoves(const Board& board, Square from, std::vector<Move>& moves);
    static void generateKingMoves(const Board& board, Square from, std::vector<Move>& moves);
    
    // 马腿检测
    static bool isKnightLegBlocked(const Board& board, Square from, Square to);
    
    // 象眼检测
    static bool isBishopEyeBlocked(const Board& board, Square from, Square to);
};

} // namespace SharkEngine

#endif // SHARK_ENGINE_MOVEGEN_H
