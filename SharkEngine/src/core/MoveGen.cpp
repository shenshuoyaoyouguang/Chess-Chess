#include "core/MoveGen.h"
#include <algorithm>

namespace SharkEngine {

std::vector<Move> MoveGenerator::generateLegalMoves(const Board& board) {
    std::vector<Move> moves;
    generatePseudoLegalMoves(board, moves);
    
    // 过滤非法走法（不能导致自己被将军）
    std::vector<Move> legalMoves;
    for (const auto& move : moves) {
        // 创建临时棋盘测试
        Board tempBoard = board;
        if (tempBoard.makeMove(move)) {
            // 走棋后检查自己是否被将军
            if (!tempBoard.isInCheck(board.sideToMove())) {
                legalMoves.push_back(move);
            }
        }
    }
    
    return legalMoves;
}

std::vector<Move> MoveGenerator::generatePseudoLegalMoves(const Board& board) {
    std::vector<Move> moves;
    
    for (int i = 0; i < 90; ++i) {
        Piece piece = board.pieceAt(Square::fromIndex(i));
        if (piece.color != board.sideToMove()) continue;
        
        Square from = Square::fromIndex(i);
        
        switch (piece.type) {
            case PieceType::PAWN:
                generatePawnMoves(board, from, moves);
                break;
            case PieceType::KNIGHT:
                generateKnightMoves(board, from, moves);
                break;
            case PieceType::BISHOP:
                generateBishopMoves(board, from, moves);
                break;
            case PieceType::ROOK:
                generateRookMoves(board, from, moves);
                break;
            case PieceType::CANNON:
                generateCannonMoves(board, from, moves);
                break;
            case PieceType::ADVISOR:
                generateAdvisorMoves(board, from, moves);
                break;
            case PieceType::KING:
                generateKingMoves(board, from, moves);
                break;
            default:
                break;
        }
    }
    
    return moves;
}

std::vector<Move> MoveGenerator::generateCaptureMoves(const Board& board) {
    std::vector<Move> captures;
    auto allMoves = generatePseudoLegalMoves(board);
    
    for (const auto& move : allMoves) {
        if (board.pieceAt(move.to).isValid()) {
            captures.push_back(move);
        }
    }
    
    return captures;
}

std::vector<Move> MoveGenerator::generateQuietMoves(const Board& board) {
    std::vector<Move> quiets;
    auto allMoves = generatePseudoLegalMoves(board);
    
    for (const auto& move : allMoves) {
        if (!board.pieceAt(move.to).isValid()) {
            quiets.push_back(move);
        }
    }
    
    return quiets;
}

bool MoveGenerator::isLegal(const Board& board, const Move& move) {
    if (!move.isValid()) return false;
    
    Piece piece = board.pieceAt(move.from);
    if (piece.color != board.sideToMove()) return false;
    
    Piece target = board.pieceAt(move.to);
    if (target.color == board.sideToMove()) return false;
    
    // 测试走棋
    Board tempBoard = board;
    if (!tempBoard.makeMove(move)) return false;
    
    // 检查是否导致自己被将军
    return !tempBoard.isInCheck(board.sideToMove());
}

bool MoveGenerator::givesCheck(const Board& board, const Move& move) {
    Board tempBoard = board;
    if (!tempBoard.makeMove(move)) return false;
    
    return tempBoard.isInCheck(board.sideToMove() == Color::RED ? Color::BLACK : Color::RED);
}

void MoveGenerator::generatePawnMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 红兵向上（rank 减小），黑卒向下（rank 增大）
    int forward = (color == Color::RED) ? -1 : 1;
    
    // 前进
    Square to(from.file, from.rank + forward);
    if (to.isValid()) {
        Piece target = board.pieceAt(to);
        if (target.color != color) {
            moves.emplace_back(from, to);
        }
    }
    
    // 过河后可以横走
    bool acrossRiver = (color == Color::RED) ? (from.rank <= 5) : (from.rank >= 6);
    if (acrossRiver) {
        // 左移
        to = Square(from.file - 1, from.rank);
        if (to.isValid()) {
            Piece target = board.pieceAt(to);
            if (target.color != color) {
                moves.emplace_back(from, to);
            }
        }
        
        // 右移
        to = Square(from.file + 1, from.rank);
        if (to.isValid()) {
            Piece target = board.pieceAt(to);
            if (target.color != color) {
                moves.emplace_back(from, to);
            }
        }
    }
}

void MoveGenerator::generateKnightMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 马走日字的 8 个方向
    static const int knightOffsets[8][4] = {
        {1, 2, 0, 1},   // 右上：先检查 (1,0) 是否有子，目标 (1,2)
        {-1, 2, 0, 1},  // 左上
        {1, -2, 0, -1}, // 右下
        {-1, -2, 0, -1},// 左下
        {2, 1, 1, 0},   // 上右
        {-2, 1, -1, 0}, // 上左
        {2, -1, 1, 0},  // 下右
        {-2, -1, -1, 0} // 下左
    };
    
    for (const auto& offset : knightOffsets) {
        int df = offset[0];
        int dr = offset[1];
        int legF = offset[2];
        int legR = offset[3];
        
        Square to(from.file + df, from.rank + dr);
        Square leg(from.file + legF, from.rank + legR);
        
        if (to.isValid() && leg.isValid()) {
            // 检查马腿
            if (!board.pieceAt(leg).isValid()) {
                Piece target = board.pieceAt(to);
                if (target.color != color) {
                    moves.emplace_back(from, to);
                }
            }
        }
    }
}

void MoveGenerator::generateBishopMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 象走田字的 4 个方向
    static const int bishopOffsets[4][3] = {
        {2, 2, 1, 1},   // 右上
        {-2, 2, -1, 1}, // 左上
        {2, -2, 1, -1}, // 右下
        {-2, -2, -1, -1}// 左下
    };
    
    for (const auto& offset : bishopOffsets) {
        int df = offset[0];
        int dr = offset[1];
        int eyeF = offset[2];
        int eyeR = offset[3];
        
        Square to(from.file + df, from.rank + dr);
        Square eye(from.file + eyeF, from.rank + eyeR);
        
        if (to.isValid() && eye.isValid()) {
            // 检查象眼
            if (!board.pieceAt(eye).isValid()) {
                // 检查是否过河
                if ((color == Color::RED && to.rank >= 6) ||
                    (color == Color::BLACK && to.rank <= 5)) {
                    Piece target = board.pieceAt(to);
                    if (target.color != color) {
                        moves.emplace_back(from, to);
                    }
                }
            }
        }
    }
}

void MoveGenerator::generateRookMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 车的 4 个方向：上下左右
    static const int directions[4][2] = {
        {0, 1}, {0, -1}, {1, 0}, {-1, 0}
    };
    
    for (const auto& dir : directions) {
        int df = dir[0];
        int dr = dir[1];
        
        int f = from.file + df;
        int r = from.rank + dr;
        
        while (true) {
            Square to(f, r);
            if (!to.isValid()) break;
            
            Piece target = board.pieceAt(to);
            if (target.color == color) break; // 遇到自己的棋
            
            moves.emplace_back(from, to);
            
            if (target.isValid()) {
                break; // 吃到对方棋子后停止
            }
            
            f += df;
            r += dr;
        }
    }
}

void MoveGenerator::generateCannonMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 炮的 4 个方向
    static const int directions[4][2] = {
        {0, 1}, {0, -1}, {1, 0}, {-1, 0}
    };
    
    for (const auto& dir : directions) {
        int df = dir[0];
        int dr = dir[1];
        
        int f = from.file + df;
        int r = from.rank + dr;
        bool jumped = false; // 是否翻山
        
        while (true) {
            Square to(f, r);
            if (!to.isValid()) break;
            
            Piece target = board.pieceAt(to);
            
            if (!jumped) {
                if (target.isEmpty()) {
                    moves.emplace_back(from, to); // 不吃子移动
                } else {
                    jumped = true; // 翻山
                }
            } else {
                if (target.isValid()) {
                    if (target.color != color) {
                        moves.emplace_back(from, to); // 吃子
                    }
                    break; // 翻山后遇到第一个棋子就停止
                }
            }
            
            f += df;
            r += dr;
        }
    }
}

void MoveGenerator::generateAdvisorMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 士的 4 个斜方向
    static const int advisorOffsets[4][2] = {
        {1, 1}, {1, -1}, {-1, 1}, {-1, -1}
    };
    
    for (const auto& offset : advisorOffsets) {
        Square to(from.file + offset[0], from.rank + offset[1]);
        
        if (to.isValid() && Board::isInPalace(to, color)) {
            Piece target = board.pieceAt(to);
            if (target.color != color) {
                moves.emplace_back(from, to);
            }
        }
    }
}

void MoveGenerator::generateKingMoves(const Board& board, Square from, std::vector<Move>& moves) {
    Piece piece = board.pieceAt(from);
    Color color = piece.color;
    
    // 将帅的 4 个方向
    static const int directions[4][2] = {
        {0, 1}, {0, -1}, {1, 0}, {-1, 0}
    };
    
    for (const auto& dir : directions) {
        Square to(from.file + dir[0], from.rank + dir[1]);
        
        if (to.isValid() && Board::isInPalace(to, color)) {
            Piece target = board.pieceAt(to);
            if (target.color != color) {
                moves.emplace_back(from, to);
            }
        }
    }
}

bool MoveGenerator::isKnightLegBlocked(const Board& board, Square from, Square to) {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    int legF = from.file;
    int legR = from.rank;
    
    if (abs(df) == 2) {
        legF = from.file + df / 2;
    } else {
        legR = from.rank + dr / 2;
    }
    
    return board.pieceAt(Square(legF, legR)).isValid();
}

bool MoveGenerator::isBishopEyeBlocked(const Board& board, Square from, Square to) {
    int df = to.file - from.file;
    int dr = to.rank - from.rank;
    
    int eyeF = from.file + df / 2;
    int eyeR = from.rank + dr / 2;
    
    return board.pieceAt(Square(eyeF, eyeR)).isValid();
}

} // namespace SharkEngine
