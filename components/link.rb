require 'components/attachment'
require 'mathcontext'

module Cpg::Components
	class Link < Attachment
		property :from, :to, :act_on, :equation, :label
		read_only :from, :to
	
		def self.new(to = nil)
			# generate a new Link object for each pair of first -> next
			return super([nil, nil]) if to == nil
		
			res = []
		
			# self connection
			if to.length == 1
				res << super([to.first, to.first])
			else		
				to[1..-1].each do |other|
					res << super([to.first, other])
				end
			end

			res.length > 1 ? res : res.first
		end
	
		def set_property(name, val)
			super
			
			if name == 'from'
				@objects[0] = val
			elsif name == 'to'
				@objects[1].unlink(self) if @objects[1] != nil
				@objects[1] = val
				@objects[1].link(self) if val
			elsif name == 'label' || name == 'equation'
				request_redraw
			end
			
			true
		end
	
		def initialize(to = [nil, nil])
			super(to)
			
			@offset = 0
		
			if to.length > 1
				self.from = @objects[0]
				self.to = @objects[1]
			
				self.to.link(self) if self.to != nil
			end
		end
		
		def <<(objs)
			objs = objs.is_a?(Enumerable) ? objs : [objs]
			
			if objs.length == 1 && !self.from && !self.to
				objs << objs[0]
			end

			if self.from == nil
				self.from = objs.shift
				@objects[0] = self.from
			end
			
			if self.to == nil && !objs.empty?
				self.to = objs.shift
				self.to.link(self)

				@objects[1] = self.to
			end
		end
	
		def removed
			self.to.unlink(self) if self.to != nil
		end
	
		def calc_control(from, to)
			dx = to[0] - from[0]
			dy = to[1] - from[1]
		
			x = from[0] + dx / 2.0
			y = from[1] + dy / 2.0
		
			same = (dy == 0 && dx == 0)
		
			# and offset it perpendicular
			dist = 1 * (@offset.to_f + 1)
		
			alpha = same ? 0 : Math.atan(dx.to_f / -dy)
			alpha += Math::PI if dy >= 0
		
			dy = Math.sin(alpha) * dist
			dx = Math.cos(alpha) * dist
		
			return x + dx, y + dy
		end
	
		def draw(ct)
			# draw arrow from o1 to o2
			return if @objects.empty? or @objects.first == nil
		
			a1 = @objects[0].allocation
			a2 = @objects[1].allocation
		
			from = [mean(a1, 0), mean(a1, 1)]
			to = [mean(a2, 0), mean(a2, 1)]
					
			ct.move_to(from[0], from[1])
		
			x, y = calc_control(from, to)

			if from[0] == to[0] and from[1] == to[1]
				# draw pretty one
				yy = (@offset + 1) + 0.5
				xx = 2
				ct.curve_to(to[0] - xx, to[1] - yy, to[0] + xx, to[1] - yy, to[0], to[1])
			else
				ct.curve_to(x, y, x, y, to[0], to[1])
			end
		
			if @selected	
				ct.set_source_rgb(0.8, 0.8, 1)
			else
				ct.set_source_rgb(0.8, 0.8, 0.8)
			end
			
			if @focus
				ct.set_dash([ct.line_width * 5, ct.line_width * 5], 0)
			end
		
			ct.line_width = ct.line_width * 2
			ct.stroke
		
			size = 0.15
		
			# draw the arrow, first move to the center, then rotate, then draw
			# the arrow
			if from[0] == to[0] and from[1] == to[1]
				x = evaluate_bezier(to[0], to[0] - xx, to[0] + xx, to[0], 0.5)
				y = evaluate_bezier(to[1], to[1] - yy, to[1] - yy, to[1], 0.5)
			
				pos = -0.5 * Math::PI
			else
				x = evaluate_bezier(from[0], x, x, to[0], 0.5)
				y = evaluate_bezier(from[1], y, y, to[1], 0.5)

				xx = to[0] - from[0]
				yy = to[1] - from[1]
		
				if xx == 0
					pos = yy < 0 ? 1.5 * Math::PI : Math::PI * 0.5
				else
					pos = Math::atan(yy.to_f / xx)
		
					pos += Math::PI if xx < 0
					pos += Math::PI * 2 if xx > 0 && yy < 0
				end
		
				pos += Math::PI * 0.5
			end
		
			ct.translate(x, y)		
			ct.move_to(0, 0)
			ct.rotate(pos)
		
			ct.rel_line_to(-size, 0)
			ct.rel_line_to(size, -size)
			ct.rel_line_to(size, size)
			ct.rel_line_to(-size, 0)
		
			ct.fill
		
			# also draw some text here
			return unless (self.equation or self.label)
			t = to_s

			e = ct.text_extents(t)
			ct.rotate(0.5 * Math::PI)
		
			ct.rotate(Math::PI) if from[0] < to[0]
		
			ct.move_to(-e.width / 2.0, -e.height * 1.5)
			ct.set_source_rgb(0, 0, 0)
			ct.show_text(t)
		end
	
		def to_s
			(self.label or self.equation).to_s
		end
	
		def mean(alloc, offset)
			return alloc[offset] + alloc[offset + 2] / 2.0
		end
	
		def distance_to_line(start, stop, point)
			x, y = *point
		
			dx = [x - start[0], stop[0] - start[0]]
			dy = [y - start[1], stop[1] - start[1]]
		
			dot = dx[0] * dx[1] + dy[0] * dy[1]
			len_sq = dx[1] * dx[1] + dy[1] * dy[1]
			param = dot / len_sq

			if param < 0
				xx = start[0]
				yy = start[1]
			elsif param > 1
				xx = stop[0]
				yy = stop[1]
			else
				xx = start[0] + param * dx[1]
				yy = start[1] + param * dy[1]
			end

			Math.sqrt((x - xx) * (x - xx) + (y - yy) * (y - yy))
		end
	
		def evaluate_bezier(p0, p1, p2, p3, t)
			((1 - t)**3) * p0 + 3 * t * ((1 - t)**2) * p1 + 3 * (t**2) * (1 - t) * p2 + (t**3) * p3
		end
	
		def rect_hittest(p1, p2, p3, p4, rect, factor)
			# different hittest strategy, take points on the curve and see if
			# they are withing the rect
			rect = rect.collect { |x| x / factor.to_f }
		
			num = 5
			(0..num).each do |t| 
				px = evaluate_bezier(p1[0], p2[0], p3[0], p4[0], t / num.to_f)
				py = evaluate_bezier(p1[1], p2[1], p3[1], p4[1], t / num.to_f)
			
				return true if rect[0] <= px && rect[2] >= px && rect[1] <= py && rect[3] >= py
			end
		
			false
		end
	
		def hittest(rect, factor)
			dx = (rect[2] - rect[0]).abs
			dy = (rect[3] - rect[1]).abs
	
			a1 = @objects[0].allocation
			a2 = @objects[1].allocation
		
			# find P0 and P3 for bezier curve
			from = [mean(a1, 0), mean(a1, 1)]
			to = [mean(a2, 0), mean(a2, 1)]
		
			# find control point P1 and P2 for bezier curve
			cx, cy = calc_control(from, to)
		
			# do piece wise linearization
			num = 5
			dist = []
		
			prevp = from
		
			if from[0] == to[0] && from[1] == to[1]
				p2 = [to[0] - 2, to[1] - ((@offset + 1) + 0.5)]
				p3 = [to[0] + 2, to[1] - ((@offset + 1) + 0.5)]
			else
				p2 = [cx, cy]
				p3 = [cx, cy]
			end
		
			if dx > 1 || dy > 1
				return rect_hittest(from, p2, p3, to, rect, factor)
			end
		
			(1..num).each do |i|
				px = evaluate_bezier(from[0], p2[0], p3[0], to[0], i / num.to_f)
				py = evaluate_bezier(from[1], p2[1], p3[1], to[1], i / num.to_f)

				l = [prevp[0], prevp[1], px, py]

				dist << distance_to_line(prevp, [px, py], [rect[0] / factor.to_f, rect[1] / factor.to_f])
				prevp = [px, py]
			end
		
			dist.min < 10 / factor.to_f
		end
	
		def self.limits
			[1, -1]
		end
	
		def simulation_reset
		end
	
		def to_xml
			super
		end
	
		def simulation_evaluate
			begin
				return {} if self.act_on == nil || self.act_on == '' || self.equation == nil || self.equation == ''
			rescue StandardError, e
				p e
			end

			# create execution context
			c = Cpg::MathContext.new(Cpg::Simulation.instance.state, self.from.state, state, {'from' => self.from, 'to' => self.to})
		
			vars = self.act_on.to_s.split(/\s*,\s*/)
			eq = self.equation.to_s.split(/\s*,\s*/, vars.length)
		
			if vars.length != eq.length
				STDERR.puts('Number of variables is not number of equations')
				return {}
			end
		
			res = {}
		
			vars.each_with_index do |on, i|
				res[on] = c.eval(eq[i])
			end
	
			return res
		end
	end
end
