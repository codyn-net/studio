require 'cairo'

module Cpg::Components::Groups
	class Oscillator < Renderer
		def initialize
			@angle = 180
			@pause = 0
			@height = 1
			
			make_patterns
		end
		
		def make_patterns
			@patouter = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patouter.add_color_stop_rgb(0, 127 / 255.0, 172 / 255.0, 227 / 255.0)
			@patouter.add_color_stop_rgb(@height, 1, 1, 1)

			@patinner = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patinner.add_color_stop_rgb(0, 193 / 255.0, 217 / 255.0, 1)
			@patinner.add_color_stop_rgb(@height * 0.8, 1, 1, 1)
		end
		
		def draw_circle(ct, radius, allocation)
			ct.arc(allocation.width / 2.0, 
				   allocation.height / 2.0,
				   radius,
				   0,
				   2 * Math::PI)
		end
	
		def draw(obj, ct)
			allocation = obj.allocation

			if allocation.height != @height
				@height = allocation.height
				make_patterns
			end
			
			ct.save
			uw = ct.line_width

			ct.set_source(@patouter)

			draw_circle(ct, allocation.width / 2.0, allocation)
			ct.fill
		
			ct.line_width = uw * 2
			ct.set_source_rgb(26 / 255.0, 80 / 255.0, 130 / 255.0)
			draw_circle(ct, allocation.width / 2.0 * 0.9 - uw * 2, allocation)
			ct.stroke_preserve
			ct.set_source(@patinner)
			ct.fill
		
			# Draw little sine thingie in there
			radius = 0.15 * allocation.width
			ct.line_width = uw
			
			ct.translate(allocation.width / 2.0, allocation.height / 2.0)
			#ct.rotate((@angle / 180.0) * Math::PI)

			ct.set_source_rgb(29 / 255.0, 71 / 255.0, 107 / 255.0)

			ct.arc(-radius, 0, radius, Math::PI, 2 * Math::PI)
			ct.stroke
		
			ct.arc(+radius, 0, radius, 0, Math::PI)
			ct.stroke
		
			ct.line_width = uw * 2
		
			# Draw the ball according to the angle
			if @angle != 180
				# Determine location on arc
				#ct.set_source_rgb(0.95, 0.95, 0.95)
				ct.set_source_rgb(1, 1, 1)

				diff = 30
				fac = (180.0 + diff) / 180.0
		
				from = [(@angle - diff) * fac, 0].max.to_rad * 2
				to = [@angle * fac, 180].min.to_rad * 2
			
				# determine the part of the first arc
				if from < Math::PI
					p1 = Math::PI + from
					p2 = Math::PI + [to, Math::PI].min
				
					ct.arc(-radius, 0, radius, p1, p2)
					ct.stroke
				end
			
				if to >= Math::PI
					p1 = [2 * Math::PI - from, Math::PI].min
					p2 = 2 * Math::PI - to
				
					ct.arc_negative(radius, 0, radius, p1, p2)
					ct.stroke
				end
			end

			ct.restore
		
			super(ct, allocation)
		end
	
		def animate(obj, ts)
			if @angle == 180
				@pause += ts
			
				if @pause >= 3000
					@angle = 0
					@pause = 0
				else
					return false
				end
			else
				@angle += 5
			end
		
			true
		end
	end
end
